# 说明

本工程为旗舰版本的测试工程。HotUpdate程序集为热更新程序集。

启动后会将BenchmarkTest运行10遍，打印耗费的时间。

出于简单起见，本示例工程仅支持Windows、MacOS之类的Standalone平台。

## 安装

请参考旗舰版本的[快速上手文档](https://hybridclr.doc.code-philosophy.com/docs/business/ultimate/quickstartunchecked)。

## 构建游戏

- 运行`HybridCLR/Generate/All`
- 打开`Build Settings`，点击`Build`
- 运行`BuildTools/BackupAOT`备份打包时生成的AOT dll，这一步非常关键不可遗漏

## 首包测试

运行刚才构建的dhe_demo.exe程序，屏幕上会打印`========= Run Unchanged BenchmarkTest`

## 热更新测试

- 运行`BuildTools/CompileHotUpdateDllsAndGenerateDHAODatas` 生成热更新dll及对应的dhao文件，并且复制到`HotUpdateDatas`目录
- 手动复制`HotUpdateDatas`目录下HotUpdate.dll.bytes和HotUpdate.dhao.bytes到构建游戏的输出目录下的`dhe_demo_Data\StreamingAssets`目录下
- 再次运行，屏幕上会打印`========= Run Changed BenchmarkTest`

