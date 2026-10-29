# Monoposto Tyre Strategy V1.0

Windows 桌面轮胎策略规划器，面向手机游戏 Monoposto。项目可直接上传到 GitHub，并由 GitHub Actions 自动构建 Windows 安装版 EXE 和免安装 ZIP。

## 功能
- 比赛圈数、单圈基准时间、进站损失、轮胎磨损率等参数输入
- 软胎 / 中胎 / 硬胎分别设置磨损率与速度差
- 比较 1 停、2 停、3 停策略，显示估算总时间与轮胎分段
- 轮胎磨损曲线图（基于当前设置的估算曲线）
- 保存、载入本地 JSON 策略配置
- 导出策略报告 TXT
- 深色赛车仪表盘风格界面

## 重要说明
Monoposto 的不同版本、赛道、天气、设置和比赛条件会影响实际磨损与圈速。本工具不是游戏内部遥测读取器；请用自己的比赛数据校准轮胎每圈磨损率与不同配方的圈速差。V1.0 使用可解释的简化模型估算，不能保证与游戏结果完全一致。

## 构建方式
### 本机
需要 Windows 和 .NET 8 SDK：
```powershell
dotnet publish src/MonopostoTyreStrategy/MonopostoTyreStrategy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### GitHub Actions
上传项目到仓库后，进入 **Actions**，运行 `Build Windows App` 工作流。成功后在工作流的 Artifacts 下载：
- `Monoposto-Tyre-Strategy-V1.0-Portable-Windows-x64.zip`
- `Monoposto-Tyre-Strategy-V1.0-Setup-Windows-x64.exe`

## 上传到 GitHub
1. 解压 ZIP。
2. 将解压后的所有文件和隐藏的 `.github` 文件夹一起上传到仓库根目录。
3. 提交到 `main` 分支，或进入 Actions 手动运行工作流。
4. 下载构建产物。

## 数据位置
用户配置保存在 `%LOCALAPPDATA%\\MonopostoTyreStrategy\\`。卸载软件不会主动删除此目录，但仍建议定期导出备份。
