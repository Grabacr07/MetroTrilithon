## MetroTrilithon

[![Build status](https://img.shields.io/appveyor/ci/Grabacr07/MetroTrilithon/master.svg?style=flat-square)](https://ci.appveyor.com/project/Grabacr07/MetroTrilithon)
[![NuGet](https://img.shields.io/nuget/v/MetroTrilithon.svg?style=flat-square)](https://www.nuget.org/packages/MetroTrilithon/)
[![License](https://img.shields.io/github/license/Grabacr07/MetroTrilithon.svg?style=flat-square)](https://github.com/Grabacr07/MetroTrilithon/blob/develop/LICENSE.txt)

Utilities for any platforms (Windows Desktop, Windows Store/Phone, etc...)

## About

要は個人用ユーティリティ群。
[MetroRadiance](https://github.com/Grabacr07/MetroRadiance) とは別に、[Livet](https://github.com/ugaya40/Livet) 依存のデスクトップ アプリ開発用に [KanColleViewer](https://github.com/Grabacr07/KanColleViewer) 等から切り出したもの。
いずれは他プラットフォーム (Windows Store/Phone) 用のコードも入れていく予定。

* IDisposable 拡張
* INotifyPropertyChanged 拡張
* カスタム コントロール
* カスタム テンプレート
* カスタム コンバーター
* シリアライズ
* 多重起動検知

## Libraries

プロジェクト内で使用している外部ライブラリ

* [StatefulModel](https://github.com/ugaya40/StatefulModel)
 - 用途 : M-V-Whatever の Model 向けインフラストラクチャ
 - ライセンス : The MIT License (MIT)
* [Livet](http://ugaya40.hateblo.jp/entry/Livet)
 - 用途 : MVVM(Model/View/ViewModel) パターン用インフラストラクチャ
 - ライセンス : zlib/libpng
