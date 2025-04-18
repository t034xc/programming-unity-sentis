# Unity Sentis - Object Detection and Classification サンプル

オブジェクト検出とクラス分類をリアルタイムで行うアプリのサンプルです.

![](https://github.com/user-attachments/assets/42e99dca-54b6-4b5a-a3e6-413d7b231b8c)

## 開発環境

- Windows 11 Pro
- Unity 6000.0.45f1
- Unity Sentis 2.1.2

## オブジェクト検出

- 検出モデルは YOLOX (https://arxiv.org/abs/2107.08430) を使用します.
- MMDetection (https://github.com/open-mmlab/mmdetection) で学習しています.
- モデルファイル (ONNX 形式) 自体は NMS を入れていません (Unity Sentis のバグ? により極端に動作が重くなるため).
- NMS は別途 Unity Sentis の `FunctionalGraph` によりモデルを作成し, 後処理として実行しています.
- MS COCO データセット (80 クラス) により学習されています.

## クラス分類

- ONNX Model Zoo (https://github.com/onnx/models) で公開されているものを使用します.
- ImageNet-1K データセット (1000 クラス) により学習されています.
- オブジェクト検出で検出された領域を crop し, 分類モデルに入力して, クラスを推論しています (top-5 を表示).

## アプリケーション 動作デモ

- WebCamera は OBS Studio の仮想 Web カメラを使用しています.
- 解像度は 1280x720, フレームレートは 24 fps です.

### YOLOX-l による検出のみ

https://github.com/t034xc/programming-unity-sentis/blob/master/demo/demo-yolox_l.mp4

### YOLOX-l による検出 + ResNet152 による分類

https://github.com/t034xc/programming-unity-sentis/blob/master/demo/demo-yolox_l-resnet152.mp4

### YOLOX-nano による検出のみ

https://github.com/t034xc/programming-unity-sentis/blob/master/demo/demo-yolox_nano.mp4

### YOLOX-nano による検出 + MobileNetV2 による分類

https://github.com/t034xc/programming-unity-sentis/blob/master/demo/demo-yolox_nano-mobilenetv2_050.mp4

## モバイルデバイスでのパフォーマンスに関して

Google Pixel7a で YOLOX-nano を使ったオブジェクト検出のみ行った場合, 検出のフレームレートは約 5 fps です (今後の課題).
