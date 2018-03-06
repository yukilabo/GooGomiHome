# GooGomiHome
GoogleHomeにゴミの品名を言うと分別種類をこたえてくれる(予定)

## TODO
- [x] 広島市の[ごみ分別50音辞典](http://www.city.hiroshima.lg.jp/www/contents/1277099413287/index.html)から分別情報をスクレイピングする
- [x] スクレイピングした情報をjsonかCSVに書き出す
- [x] Actions on Googleでアクションを作成
- [x] DialogFlowでユーザーが話した品名から分別種別を特定し回答する

## スクレイピング
広島市の分別情報のテーブルは構造がおかしなところが多々あり、すべて正常にスクレイピングしようとすると、手間がかかりすぎるので、ある程度のところで妥協。

## Actions on Google
プロジェクトを作成して、適当に情報を埋める。
アクションはDialogFlowを選択する。

## DialogFlow
EntitiesのUpload Entitiesで、作成したCSVをアップロード
ゴミの分別ごとにIntentを作成することで、DialogFlowに分別を判断させる。

## License
   Copyright 2018 yukilabo

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
   
