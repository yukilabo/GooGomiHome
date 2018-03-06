using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace BunbetsuScraper
{
	class Program
	{
		private const string PAGE_URL = @"http://www.city.hiroshima.lg.jp/www/contents/1277099413287/index.html";

		static void Main(string[] args)
		{
			// インデックスページから詳細ページのURLのリストを抽出
			var details = GetDetailUrls(PAGE_URL);

			// 各詳細ページから分別情報を抽出
			var gomiList = details.SelectMany(t => GetBunbetsuItems(t))
								  .Where(t => string.IsNullOrEmpty(t.Name1) == false)
								  .GroupBy(t => t.Type).ToList();


			// 抽出結果を出力
			foreach (var gomiType in gomiList)
			{
				var fileName = GetFileNameWithType(gomiType.Key);
				using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
				{

					Console.WriteLine(gomiType.Key);
					Console.WriteLine("===================================");

					foreach (var item in gomiType)
					{
						var name = string.Format("{0}{1}", item.Name1, item.Name2)
										 .Replace("(", "（")
										 .Replace(")", "）");
						writer.WriteLine(name);
						Console.WriteLine(name);
					}
					Console.WriteLine();
					Console.WriteLine();
				}
			}
			var text = Console.ReadLine();
		}

		/// <summary>
		/// Indexページをスクレイピングして詳細ページのURLを抽出する
		/// </summary>
		/// <param name="indexUrl">IndexページのURL</param>
		/// <returns></returns>
		static List<string> GetDetailUrls(string indexUrl)
		{
			var doc = GetHtmlDocument(indexUrl);

			// indexページ内の各行(ア行、カ行)へのリンクを抽出
			var items = doc.QuerySelectorAll("a[href*='#']")
				.Select(t => t.GetAttribute("href"))
				.Select(t => t.Split('#').First())
				.Where(t => t.Length > 0)
				.Distinct();

			return items.ToList();
		}

		/// <summary>
		/// 詳細ページをスクレイピングして分別情報を抽出する
		/// </summary>
		/// <param name="detailUrl"></param>
		/// <returns></returns>
		private static List<Gomi> GetBunbetsuItems(string detailUrl)
		{
			var doc = GetHtmlDocument(detailUrl);

			var trList = doc.QuerySelector("#Main")
						   .QuerySelectorAll("table table")[2]
						   .QuerySelectorAll("tr")
						   .Where(t => IsHeader(t) == false && IsSeparator(t) == false)
						   .ToArray();

			var items = Enumerable.Range(0, trList.Length).Select(t => new Gomi()).ToList();

			for (int index = 0; index < trList.Length; index++)
			{
				var pos = 0;

				// 頭文字
				var trElement = trList[index];
				if (string.IsNullOrWhiteSpace(items[index].Initial))
				{
					var rowspan = GetRowSpan(trElement, pos);
					var text = trElement.Children[pos].TextContent;
					foreach (var item in items.Skip(index).Take(rowspan))
					{
						item.Initial = text;
					}
					pos++;
				}

				// 名前1
				if (items[index].Name1 == null)
				{
					if (GetColSpan(trElement, pos) > 2)
					{
						continue;
					}

					var rowspan = GetRowSpan(trElement, pos);
					var colspan = GetColSpan(trElement, pos);
					var text = GetNormalizedText(trElement, pos);
					foreach (var item in items.Skip(index).Take(rowspan))
					{
						item.Name1 = text;
						if (colspan == 2)
						{
							item.Name2 = "";
						}
					}
					pos++;
				}

				// 名前2
				if (items[index].Name2 == null)
				{
					var rowspan = GetRowSpan(trElement, pos);
					var text = GetNormalizedText(trElement, pos);
					foreach (var item in items.Skip(index).Take(rowspan))
					{
						item.Name2 = text;
					}
					pos++;
				}

				// 分類
				if (items[index].Type == null)
				{
					var rowspan = GetRowSpan(trElement, pos);
					var text = GetNormalizedText(trElement, pos);
					foreach (var item in items.Skip(index).Take(rowspan))
					{
						item.Type = text;
					}
					pos++;
				}

				// メモ
				if (items[index].Note == null)
				{
					if (trElement.ChildElementCount >= pos)
					{
						continue;
					}

					var rowspan = GetRowSpan(trElement, pos);
					var text = GetNormalizedText(trElement, pos);
					foreach (var item in items.Skip(index).Take(rowspan))
					{
						item.Note = text;
					}
					pos++;
				}

			}

			return items;
		}

		/// <summary>
		/// itemのpos番目の子要素のrowspanの値を取得する。
		/// </summary>
		/// <param name="item"></param>
		/// <param name="pos"></param>
		/// <returns>rowspanの値。指定されていなければ1を返却する</returns>
		private static int GetRowSpan(IElement item, int pos)
		{
			if (item.Children[pos].HasAttribute("rowspan"))
			{
				return Convert.ToInt32(item.Children[pos].GetAttribute("rowspan"));
			}

			return 1;
		}

		/// <summary>
		/// itemのpos番目の子要素のcolspanの値を取得する。
		/// </summary>
		/// <param name="item"></param>
		/// <param name="pos"></param>
		/// <returns>colspanの値。指定されていなければ1を返却する</returns>
		private static int GetColSpan(IElement item, int pos)
		{
			if (item.Children[pos].HasAttribute("colspan"))
			{
				return Convert.ToInt32(item.Children[pos].GetAttribute("colspan"));
			}

			return 1;
		}

		/// <summary>
		/// itemのpos番目の子要素のTextContentの値を改行と前後の空白を除外する。
		/// </summary>
		/// <param name="item"></param>
		/// <param name="pos"></param>
		/// <returns>TextContentを改行で分割し前後の空白を除外して連結した文字列</returns>
		private static string GetNormalizedText(IElement item, int pos)
		{
			return string.Join("", item.Children[pos].TextContent.Split("\n").Select(t => t.Trim()));
		}

		/// <summary>
		/// テーブルの項目名のtrタグかどうか
		/// </summary>
		/// <param name="item">trタグのエレメント</param>
		/// <returns>品名が2列のアイテムだったらtrue</returns>
		private static bool IsHeader(IElement item)
		{
			return item.FirstElementChild.TextContent == "品目";
		}

		/// <summary>
		/// テーブルの間のtrタグかどうか
		/// </summary>
		/// <param name="item">trタグのエレメント</param>
		/// <returns>テーブルの間のtrタグだったらtrue</returns>
		private static bool IsSeparator(IElement item)
		{
			return item.FirstElementChild.GetAttribute("colspan") == "6";
		}

		/// <summary>
		/// 指定したURLのHTMLをパースする
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns></returns>
		private static IHtmlDocument GetHtmlDocument(string url)
		{
			// 指定したサイトのHTMLをストリームで取得する
			var doc = default(IHtmlDocument);
			using (var client = new HttpClient())
			using (var stream = client.GetStreamAsync(new Uri(url)).Result)
			{
				// AngleSharp.Parser.Html.HtmlParserオブジェクトにHTMLをパースさせる
				var parser = new HtmlParser();
				doc = parser.ParseAsync(stream).Result;
			}

			return doc;
		}

		/// <summary>
		/// ゴミの分別種類に応じたファイル名を返却する
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static string GetFileNameWithType(string type)
		{
			var fileName = "other.csv";
			switch (type)
			{
				case "可燃ごみ":
					fileName = "burnable.csv";
					break;
				case "ペットボトル":
					fileName = "petbottle.csv";
					break;
				case "リサイクルプラ":
					fileName = "recycle_pla.csv";
					break;
				case "その他プラ":
					fileName = "other_pla.csv";
					break;
				case "不燃ごみ":
					fileName = "unburnable.csv";
					break;
				case "資源ごみ":
					fileName = "resource.csv";
					break;
				case "有害ごみ":
					fileName = "hazardous.csv";
					break;
				case "大型ごみ":
					fileName = "large.csv";
					break;
				case "家電リサイクル法対象機器":
					fileName = "kaden_recycle.csv";
					break;
			}
			return fileName;
		}
	}
}
