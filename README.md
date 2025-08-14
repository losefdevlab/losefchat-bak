# LosefChat | 一款跨桌面平台的创意命令行聊天

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->

[![All Contributors](https://img.shields.io/badge/all_contributors-2-orange.svg?style=flat-square)](#contributors-)

<!-- ALL-CONTRIBUTORS-BADGE:END -->

CTCC社区*创作内容 - 自由创作性质

主要开发：阿龙<br>
Copyright (C) 2025 **LosefDevLab**<br>
Copyright (C) 2018-Now **XYLCS/XIT**<br>
Copyright (C) 2019-2025 **kakako Chat Community Studio(KCCS)**<br>
Copyright (C) 2024-2025 **PPPO Technological Team(PTT)**<br>

以上合并称为 LosefChat开发团队<br>

Losefchat (2025) by LosefChat开发团队 2025-Now freedom-create in XFCSTD2.<br>

XFCSTD PATH:/XFCSTD2.md<br>


> **注意**：本项目需要自行编译，请参考“如何编译？”H2标题处。<br>
>
> **注意**：本项目的操作请参考“如何使用？”H2标题处。
> 
> **注意**：我们会发布一些相关的公告在ANNOUNCEMENT.md文件中。请注意查看。

## License

LosefChat 使用 MIT License

## 主要内容

- 🖥️跨桌面平台
- 1️⃣模块化°
- 🧩支持MOD
- 🛜IPV4/IPV6
- 🛡️给开发者提供了安全通讯工具
- #️⃣命令行
- 🧊占用低
- 🔐密码功能
- 🔏Pefender密码防破解
- ⌨️ACIO(人工的控制命令行IO)来解决命令行I和O互相干扰²
- 😊良好的UUE(User Use Experience, 用户使用体验)
- 📱服务端管理
- ⬆️开放, 自由

> [!IMPORTANT]
> 适用于任何桌面端平台，但目前已知不适用于Android平台。LosefChat可能不会在移动端平台成功编译或运行

## 模组支持

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
openssl x509 -req -days 365 -in sfc.csr -signkey sfc.key -out sfc.crt
# ^^^签名认证
openssl pkcs12 -export -out sfc.pfx -inkey sfc.key -in sfc.crt
# ^^^导出
# 以上内容仅服务端需要操作, 客户端连接到这种开启安全通讯的服务器需要在程序同目录下提供服务器开放的安全通讯证书
```

然后请执行以下操作:

### **Windows**:

要连接服务器，请直接启动本程序,  然后填好预设,  然后在另一个命令行中用以下的方式启动losefchat,  这个将作为你输入消息的地方:
`.\losefchat -ci`

这样你才能输出消息，别认为他麻烦,   否则你正在打消息的时候，别人一条消息发过来，把你的消息编辑体验搞得一团糟你就要骂娘了

当然，如果有能力的话，也可以像下面一样安装Tmux,  编写脚本，以至于下次更方便的启动

### **Linux/macOS**

同样要类似Windows那样操作，但是更方便的操作是请使用Tmux,   这就是意味着你需要安装Tmux

然后请在程序同目录下这样编写一个脚本,  命名为lachcl.sh

```b
#!/bin/bash
tmux new-session -s losefchat -d
tmux send-keys -t losefchat:0 './losefchat' C-m
tmux split-window -v -t losefchat
tmux send-keys -t losefchat:0.1 './losefchat -ci' C-m
tmux attach -t losefchat
```

执行`chmod +x lachcl.sh`

然后现在你先按下`Ctrl + B`,   然后松开,  然后按上键,  切换到上面的窗格,   这时候你就可以在上面那一部分输入1进入客户端

像上面的方法一样切换到下面的窗格,   这时候你已经可以进行发消息聊天了,上面是消息显示，下面是消息输入

(但是这种方法有一种缺点，就是你需要提前进行编写preset.txt)

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
  -----
  - (更新简述)
  ```

## 如何使用？

### 服务端

先提前在运行目录下填写好config.txt，格式如下：

```
服务器所使用的或对方客户端连接(如服务器IP公网情况下)的端口
服务器名称
服务器描述
```

运行LosefChat，输入2，回车键。

现在你可以输入/help了解如何控制服务端。

> [!IMPORTANT]
>
> 强制开启白名单模式，除非修改源代码，否则必须将对方用户名添加白名单，否则对方无法进入

### 客户端

运行LosefChat，如果是首次运行或者某些情况，那么请按照他的要求做。没看到preset.txt的原因是可能在运行目录里面而并非软件目录。

如果你完全满足以下3个条件，无一不满足：在白名单内、没有被对方服务器封禁、通过了LC客户端合法验证、preset.txt文件存在并且正常格式，则能成功且正常加入服务器聊天。

至于如何输入、编辑、发送消息，请看“如何编译？”H2标题处。

## 注释

*：CTCC ： Chat Technological Creator Community，聊天科技创作社区

°：模块化是指功能并不直接掺在Main类里面，也不直接掺在一个方法里面，且客户端和服务端、Main类分开类。并且有一个类就是一个模块的概念。

²：仅限服务端



## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->

<!-- prettier-ignore-start -->

<!-- markdownlint-disable -->

<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Alonglosef"><img src="https://avatars.githubusercontent.com/u/200359803?v=4?s=100" width="100px;" alt="阿龙"/><br /><sub><b>阿龙</b></sub></a><br /><a href="https://github.com/losefdevlab/losefchat/commits?author=Alonglosef" title="Code">💻</a> <a href="#security-Alonglosef" title="Security">🛡️</a> <a href="https://github.com/losefdevlab/losefchat/commits?author=Alonglosef" title="Tests">⚠️</a> <a href="#design-Alonglosef" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://www.xylcsstudio.com"><img src="https://avatars.githubusercontent.com/u/158823035?v=4?s=100" width="100px;" alt="XYLCS-Studio"/><br /><sub><b>XYLCS-Studio</b></sub></a><br /><a href="https://github.com/losefdevlab/losefchat/commits?author=XYLCS-Studio" title="Code">💻</a> <a href="https://github.com/losefdevlab/losefchat/issues?q=author%3AXYLCS-Studio" title="Bug reports">🐛</a> <a href="#design-XYLCS-Studio" title="Design">🎨</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->

<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
