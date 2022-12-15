# Differential Hybrid Execution 示例项目

本项目演示 [Differential Hybird Execution](https://focus-creative-games.github.io/hybridclr/differential_hybrid_execution/) （差分混合执行技术）使用。

使用本项目前，请先掌握HybridCLR的基础使用，参见 [hybridclr_trial](https://github.com/focus-creative-games/hybridclr_trial)。

**示例项目使用 Unity 2020.3.33(任意后缀子版本如f1、f1c1、f1c2都可以) 版本**，2019.4.x、2020.3.x、2021.3.x系列都可以，但为了避免新手无谓的出错，尽量使用2020.3.33版本来体验。

## 介绍

- HotUpdateMain 类中 RunNotChangedMethod为未变化的函数，应该以AOT方式执行
- RunChangedMethod为变化的函数，应该以解释方式执行。为了模拟代码修改后的样子，使用 DHE_HOT_UPDATE 宏来区分原始和修改后的代码。
- 正常的项目，直接`HybridCLr/CompileAll/ActiveBuildTarget`即可编译热更新dll，而示例项目出于对比目的，使用 DHE_HOT_UPDATE 宏来区分原始和热更新的代码，因此编译热更新dll需要加上DHE_HOT_UPDATE宏定义。使用`HybridCLR/CompileDHEDlls`编译出热更新dll。

## 打包

- `HybridCLR/Build/Win64` 生成并且运行示例项目，可以看到 RunNotChangedMethod和RunChangedMethod虽然原始代码相同，但经过变更后，RunChangedMethod以解释方式执行，执行时间有近8倍的差距。

## 其他说明

DHE暂时只提供商业版本，不开源源码，但项目Release中附带了一个通用的ios版本的libil2cpp.a，可以用于一般性质的测试体验，替换导出的xcode工程的libil2cpp.a即可。

更详细请参见 [Differential Hybird Execution](https://focus-creative-games.github.io/hybridclr/differential_hybrid_execution/) 。


## 支持与联系

- 官方1群：651188171。可以反馈bug，但**不要在群里咨询基础使用问题**。
- 官方2群：680274677。可以反馈bug，但**不要在群里咨询基础使用问题**。
- 新手1群：428404198。新手使用过程中遇到问题，都可以在群里咨询。
- 官方邮箱：hybridclr@focus-creative-games.com
- 商业合作邮箱: business@focus-creative-games.com