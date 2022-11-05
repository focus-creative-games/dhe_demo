# Differential Hybrid Execution 示例项目

本项目演示 [Differential Hybird Execution](https://focus-creative-games.github.io/hybridclr/differential_hybrid_execution/) （差分混合执行技术）使用。

使用本项目前，请先掌握HybridCLR的基础使用，参见 [hybridclr_trial](https://github.com/focus-creative-games/hybridclr_trial)。

**示例项目使用 Unity 2020.3.33(任意后缀子版本如f1、f1c1、f1c2都可以) 版本**，2019.4.x、2020.3.x、2021.3.x系列都可以，但为了避免新手无谓的出错，尽量使用2020.3.33版本来体验。

## 设置

菜单 `HybridCLR/Settings` 中

- differentialHybridAssemblies 差分混合执行的assembly列表
- differentialHybridOptionOutputDir 差分混合执行的assembly的配置数据

## 相关生成命令

- `HybridCLR/Generate/Il2CppDef` 生成一些Unity版本相关宏
- `HybridCLR/Generate/DHEAssemblyList` 生成 差分混合执行的assembly列表
- `HybridCLR/Generate/DHEAssemblyOptionData` 生成差分混合执行assembly的一些配置数据

## 打包

- `HybridCLR/Generate/All` 生成所有
- `HybridCLR/Build/Win64` 生成并且运行示例项目

## 运行

示例项目中 Assebmly-CSharp.dll 为差分混合执行的assembly。

该assembly中仅 CreateByCode::Start 函数以AOT方式执行，剩余所有代码以解释方式执行。

运行程序后，主界面上显示两个按钮：

- 运行原始AOT dll。 对应没有发生热更的情形，直接运行原始AOT代码。
- 运行DifferentialHybrid dll。 对应发生热更的情形，一部分代码以AOT方式执行，一部分代码以解释方式执行。

## 体验差分热更新 

- 修改 HotUpdateMain中的代码
- 修改 CreateByCode::Start中代码，假设改成 `Debug.Log("这个函数在解释器中执行");`
- 运行 `HybridCLR/Build/BuildAssetsAndCopyToStreamingAssets`
- 运行 `HybridCLR/Generate/DHEAssemblyOptionData`
- 将 `Assets/StreamingAssets` 目录下的 Assembly-CSharp.dll.bytes和Assembly-CSharp.dhao.bytes 复制到 刚才打包的程序的StreamingAssets目录下
- 再次运行。点击 `运行DifferentialHybrid dll`，会发现 HotUpdateMain中打印的字符串变化了，但 CreateByCode中打印的字符串仍然为`这个函数应该在AOT中执行`执行

## 其他说明

社区版本目前只是最基本的特性演示，因此极不完善，请参见 [Differential Hybird Execution](https://focus-creative-games.github.io/hybridclr/differential_hybrid_execution/) 了解基础版本的详细用法及限制。

完善的版本随着我们专利申请进程会逐渐发布。

## 支持与联系

- 官方1群：651188171。可以反馈bug，但**不要在群里咨询基础使用问题**。
- 官方2群：680274677。可以反馈bug，但**不要在群里咨询基础使用问题**。
- 新手1群：428404198。新手使用过程中遇到问题，都可以在群里咨询。
- 官方邮箱：hybridclr@focus-creative-games.com
- 商业合作邮箱: business@focus-creative-games.com