using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace BunbetsuScraper
{
	class Program
	{
		private const string PAGE_URL = @"http://www.city.hiroshima.lg.jp/www/contents/1277099413287/index.html";

		static void Main(string[] args)
		{
			var details = GetDetailUrls(PAGE_URL);

			details.ForEach(t => GetBunbetsuItems(t));
		}

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

		private static void GetBunbetsuItems(string detailUrl)
		{
			var doc = GetHtmlDocument(detailUrl);

			var items = doc.QuerySelector("#Main")
				.QuerySelectorAll("table table")[2]
				.QuerySelectorAll("tr");

			foreach (var item in items)
			{
				if (IsHeader(item) || IsSeparator(item))
				{
					continue;
				}

				var itemName = "";
				if (IsTopItem(item))
				{
					itemName = item.Children[1].TextContent;
				}
				else if (IsDetailTopItem(item))
				{
					itemName = item.Children[1].TextContent;
				}
				else if (IsNormalItem(item))
				{
					itemName = item.FirstElementChild.TextContent;
				}
				else if (IsDetailItem(item))
				{
					itemName = item.Children[0].TextContent;
				}
				else
				{
					itemName = item.Children[0].TextContent;
				}

				itemName = string.Join("", itemName.Split("\n").Select(t => t.Trim()));

				if (string.IsNullOrWhiteSpace(itemName))
				{
					continue;
				}

				Console.WriteLine(itemName);
			}
		}

		private static bool IsDetailItem(IElement item)
		{
			return item.ChildElementCount == 5
				&& item.FirstElementChild.HasAttribute("rowspan")
				&& item.FirstElementChild.TextContent.Length > 1;
		}

		private static bool IsNormalItem(IElement item)
		{
			return item.ChildElementCount == 4
				&& item.FirstElementChild.HasAttribute("colspan");
		}

		private static bool IsTopItem(IElement item)
		{
			return item.ChildElementCount == 5 
				&& item.FirstElementChild.HasAttribute("rowspan")
				&& item.FirstElementChild.TextContent.Length == 1;
		}

		private static bool IsDetailTopItem(IElement item)
		{
			return item.ChildElementCount == 6
				&& item.FirstElementChild.HasAttribute("rowspan")
				&& item.FirstElementChild.TextContent.Length == 1;
		}

		private static bool IsHeader(IElement item)
		{
			return item.FirstElementChild.TextContent == "品目";
		}

		private static bool IsSeparator(IElement item)
		{
			return item.ChildElementCount == 1;
		}

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
	}
}
