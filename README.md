# LosefChat | 一款简易的 DotNet 聊天

主要开发：阿龙<br>
于2025.4.19正式停止官方开发,只接受社区开发,新的官方开发仍然保持原来的repo网址<br>
Copyright (C) 2025 LosefDevLab<br>
Copyright (C) 2018-Now XYLCS/XIT<br>
Copyright (C) 2019-2025 kakako Chat Community Studio(KCCS)<br>
Copyright (C) 2024-2025 PPPO Technological Team(PTT)<br>

以上合并称为 LosefChat开发团队<br>

Losefchat (2025) by LosefChat开发团队 2025-Now freedom-create in XFCSTD2.<br>

使用MIT许可证.<br>

XFCSTD PATH:/XFCSTD2.md

> **注意**：本项目需要自行编译，请参考文末的编译指南。<br>

## 1 主要内容

- 1️⃣模块化
- 🧩支持MOD
- 🛜IPV4/IPV6
- 🛡️给开发者提供了安全通讯工具
- #️⃣命令行
- 🧊占用低
- 🔐密码功能
- 🔏Pefender密码防破解
- 😊良好的UUE(User Use Experience, 用户使用体验)
- 📱服务端管理
- ⬆️开放, 自由

## 2 部署极其方便

- **启动服务器**
  输入 `2`，然后输入端口号即可启动服务器。
- **内网穿透**
  如果您的电脑本身处于公网环境，则无需配置内网穿透。
- **命令控制**
  简单输入一小段文本即可对服务器进行操作。
- **连接客户端**
  输入 `1`，然后按提示操作，开始聊天。

## 3 模组支持

LosefChat 提供了模组支持功能：

1. 拉取 LosefChat 的代码。
2. 将模组代码按模组作者要求复制到模组开发区域。(如果模组作者有自动安装程序，则使用)
3. 在模组运行区域添加安装代码。
4. 重新编译 LosefChat，即可使用第三方功能。

---

## 如何编译？

请确保已安装 `dotnet` （版本 >= 8.0）、`openssl`（版本 >= 3.0）和 `git`，然后按照以下步骤操作：

```bash
git clone https://github.com/LosefDevLab/losefchat.git
cd losefchat
dotnet build
cd bin
cd Debug
cd net8.0
# 以下内容，仅需要安全通信服务端需要操作
openssl genpkey -algorithm RSA -out sfc.key -aes256
# ^^^生成密钥
openssl req -new -key sfc.key -out sfc.csr
# ^^^生成签名
# 此处仅为演示, 实际建议使用可靠的签名, 否则早晚被破解
# 微软40块钱一份的签名它不香吗？
# 以上内容仅服务端需要操作, 客户端连接到这种开启安全通讯的服务器需要在程序同目录下提供服务器开放的安全通讯证书
openssl x509 -req -days 365 -in sfc.csr -signkey sfc.key -out sfc.crt
# ^^^签名认证
openssl pkcs12 -export -out sfc.pfx -inkey sfc.key -in sfc.crt
# ^^^导出
./losefchat
```

---

## 贡献&Git规范标准

CMTMSG规范

- 需要先创建当前要做的内容的计划issues, 这个issues可以是需要修复的、更新计划、功能添加(类似于JIRA工单)
- 当做完这些内容/做了新内容的其中一部分/修改新内容的部分/修复一些bug/合并, 按情况分别提交cmtmsg:
  - Add for #x : xxxxxxx
  - Add part for #x : xxxxxxx
  - Update for #x : xxxxxxx
  - Fix in #x : xxxxxxx
  - Merge branch xx(branchname) to xx(branch) in #x : xxxxxx

Release & tag信息规范

- 请使用WVPB版本号规则
- tag无需标注
- Release标题为版本号
- Release描述需用MD格式
- Release描述需按照以下格式进行编写:

  ```
  本次更新:
  -----
  - (本次阶段的所有CMTMSG)
  ```
