# 构建甜玉米

直接玩可以下载 [Releases](https://github.com/Johnny-son/Tym_mod/releases) 中的 `SweetCorn-1.1.0.zip`，把里面的 `FriendSpire` 文件夹解压到游戏的 `mods` 目录。游戏内同时启用甜玉米和 BaseLib。

## 在本地开发

v1.1 使用 .NET 9 SDK、游戏 v0.113.1（Steam 的 `public-beta` 分支）、BaseLib 3.4.8，以及与游戏配套的 MegaDot 4.5.1。卡牌数值和升级在 `src/FriendSpire/src/Cards.cs`，形态结算在 `Stances.cs`，角色及遗物在 `SweetCorn.cs`。修改效果时，同步更新 `assets/FriendSpire/localization/` 下的中英文描述。

1. 进入仓库的 `src/FriendSpire` 目录。
2. 设置环境变量 `Sts2DataDir`，值为本机游戏中含有 `sts2.dll`、`GodotSharp.dll` 和 `0Harmony.dll` 的目录。设置 `BaseLibDir` 为含有 `BaseLib.dll` 的目录。
3. 将 MegaDot 编辑器的可执行文件加入 PATH。下文用 `godot` 表示该可执行文件，可替换为本机的完整路径。

从该目录运行以下命令，将最后一个参数换成可写的绝对输出路径。

```sh
dotnet build -c Release
godot --headless --editor --import --path assets
godot --headless --path assets --script ../tools/pack.gd -- "/absolute/path/FriendSpire.pck"
```

将生成的 `bin/Release/net9.0/FriendSpire.dll`、`FriendSpire.pck` 和项目里的 `FriendSpire.json` 一起放进游戏的 `mods/FriendSpire/`，重启游戏即可测试。macOS 的 `mods` 在 `SlayTheSpire2.app/Contents/MacOS/` 内。

[BaseLib 3.4.8](https://github.com/Alchyr/BaseLib-StS2/releases/tag/v3.4.8) · [角色设计与卡表](design.md)
