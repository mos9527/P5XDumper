# P5XDumper
女神异闻录: 夜幕魅影 Windows 平台 `_vfileContent` 资源解包

# Download
移步 [Releases](https://github.com/mos9527/P5XDumper/releases)

# Usage
```bash
P5XDumper [OutputFolder] [ClientFolder]
```
注：`ClientFolder` 即安装目录下 `Client` 文件夹

# Notes
Unity 2022.3.12f1

# Credit
https://github.com/barncastle/AresChroniclesDumper

# Addendum
## 收集 .bundle 文件所需依赖
游戏内资源存储并不集中，读取单个`bundle`会不可避免地设计到其他依赖

同时，利用现存工具([AssetStudio](https://github.com/Perfare/AssetStudio),[AssetRipper](https://github.com/AssetRipper/AssetRipper)) 读取全部数据会不可避免地造成 OOM 问题

本项目附[CabCollector.py](https://github.com/mos9527/P5XDumper/blob/master/CabCollector.py)可以做到搜集某个`bundle`（包括自身）的所有依赖，最小化读取成本

### 使用
- 安装依赖
```bash
pip install UnityPy
```
- 构造CAB依赖图cache
```bash
python CabCollector.py --rebuild --path [dump]\InnerPackage\Bundles\Windows 
```
- 样例：提取人物模型资源

由于cache保存在工作目录，需要在构造cache的工作目录执行
```bash
python CabCollector.py --dest "models" "part_xxx_1.bundle" "part_xxx_2.bundle" "..."
```
