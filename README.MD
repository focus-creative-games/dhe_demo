# 说明

本工程为旗舰版本的测试工程。HotUpdate程序集为热更新程序集。

启动后会运行Benchmark中测试，对Test1和Test2各运行10遍，打印耗费的时间。

## 首包测试

直接运行`Release/dhe_demo2.exe`，由于此时未发生任何修改，Test1和Test2都执行了AOT版本，运行时间相同，而且运行时都为0（四舍五入导致）。

## 热更新测试

`Release\dhe_demo2_Data`目录下 StreamingAssets_v0 为首包代码， StreamingAssets_v1为热更新后代码。初始时， StreamingAssets目录内容与StreamingAssets_v0相同。

- 打开`Release\dhe_demo2_Data`目录
- 删除StreamingAssets目录
- 将StreamingAssets_v1复制为StreamingAssets
- 再次运行 `Release/dhe_demo2.exe`，此时Test0函数未修改，仍然以AOT模式执行，而Test2函数发生变化，以解释模式执行。从打印的运行时间日志，可以明显看到区别

