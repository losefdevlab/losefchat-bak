using LosefDevLab.LosefChat.lcstd;
// Mod : Boot, Des.: LosefChat 启动器
public partial class LosefChatPlatfrom
{
    static void Main(string[] args)
    {
        string inputFilePath = ".ci";
        if (!File.Exists(inputFilePath))
        {
            using (File.Create(inputFilePath)) { }
        }

        if (args.Length > 0 && args[0] == "-ci")
        {
            Console.WriteLine(@"  _                             __    ____   _               _");
            Console.WriteLine(@" | |       ___    ___    ___   / _|  / ___| | |__     __ _  | |_");
            Console.WriteLine(@" | |      / _ \  / __|  / _ \ | |_  | |     | '_ \   / _` | | __|");
            Console.WriteLine(@" | |___  | (_) | \__ \ |  __/ |  _| | |___  | | | | | (_| | | |_");
            Console.WriteLine(@" |_____|  \___/  |___/  \___| |_|    \____| |_| |_|  \__,_|  \__|");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("LosefChat Client 纯输入模式");
            while (true)
            {
                Console.Write("> ");
                string? cinp = Console.ReadLine();
                if (cinp == "exit")
                {
                    Environment.Exit(0);
                }
                else if (cinp?.Length > 1000)
                {
                    Console.Clear();
                    Console.Write("输入过长，请重新输入。");
                    Thread.Sleep(1000);
                    continue;
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(inputFilePath, true))
                    {
                        sw.WriteLine(cinp);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine(@"  _                             __    ____   _               _");
            Console.WriteLine(@" | |       ___    ___    ___   / _|  / ___| | |__     __ _  | |_");
            Console.WriteLine(@" | |      / _ \  / __|  / _ \ | |_  | |     | '_ \   / _` | | __|");
            Console.WriteLine(@" | |___  | (_) | \__ \ |  __/ |  _| | |___  | | | | | (_| | | |_");
            Console.WriteLine(@" |_____|  \___/  |___/  \___| |_|    \____| |_| |_|  \__,_|  \__|");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("欢迎使用LosefChat 4.0.d1.b72客户端请注意:正常启动后，仅输出，输入模式请另启动程序（使用-ci附加参数启动程序）\n输入1 开始聊天,输入2 server,输入3 EXIT,输入4发行说明");
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out int choose))
                {
                    Console.WriteLine("无效输入，请输入1、2、3或4。");
                    return;
                }

                if (choose == 1)
                {
                    Client client = new Client();

                    Preset preset = new Preset();
                    preset.ReadPreset();

                    client.Connect(preset.ipvx, preset.ip, preset.port, preset.username, preset.password);
                }
                else if (choose == 2)
                {
                    Config config = new Config();
                    config.ReadConfig();
                    LosefDevLab.LosefChat.lcstd.Server server = new LosefDevLab.LosefChat.lcstd.Server(config.port, config.sn, config.sd);
                    server.Start();
                }
                else if (choose == 3)
                {
                    Environment.Exit(0);
                }
                else if (choose == 4)
                {
                    Console.WriteLine(@"Losefchat 4.0.d1.b72 发行说明：");
                    Console.WriteLine(@"-------------------------------------");
                    Console.WriteLine(@"-------------------------------------");
                    Console.WriteLine(@"GitHub：https://github.com/losefdevlab/losefchat");
                    Console.WriteLine(@"Email:along-losef@outlook.com");
                    Console.WriteLine(@"-------------------------------------");
                    Console.WriteLine(@"Losefchat使用了MIT许可证，这意味着你在搬运或复制并发布这个软件的修改或者副本的时候，无需公开源代码"+
                                      "，但是必须以方便用户可视的方式保留以下原文（以下原文也作为内置在软件中的用户可视的Losefchat的MIT许可证源文本）：");
                    Console.WriteLine(@"The MIT License (MIT)

Copyright (C) 2025 LosefDevLab
Copyright (C) 2018-Now XYLCS/XIT
Copyright (C) 2019-2025 kakako Chat Community Studio(KCCS)
Copyright (C) 2024-2025 PPPO Technological Team(PTT)

以上合并称为 LosefChat开发团队<br>

Losefchat (2025) by LosefChat开发团队 2025-Now freedom-create in XFCSTD2.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
                    ");
                    Console.WriteLine(@"


4
4-------------------------------------");
                    Console.WriteLine(@"LosefChat使用了自由创作者准则XFCSTD2，这意味着你在本项目上搭建的一切公开社区或者行使的一切社区行为都需要遵守XFCSTD2条款，以下是XFCSTD2原文：");
                    Console.WriteLine(@"### XYLCS STUDIO 制定的 自由创作者标准 2

### XYLCS Freedom Create Standard 2

##### (XFCSTD2 EditVer1)

[XFCSTD2 EditVer1(2025) by XYLCS STUDIO 2018-2025 自由创作 in XFCSTD2.]

---

## 第一章 定义与适用范围

### 1.1 核心术语定义

**作品**：指创作者创作的智力成果，包括但不限于软件、文学、艺术作品及其他创造性表达形式。
**自由创作性质**:指本标准全部条款所呈现的创作者性质.具体概括为自由且开放的进行创作活动的一种创作性质.
**自由创作作品**：指符合本标准全部条款的智力成果，包括但不限于软件、文学、艺术作品及其他创造性表达形式。

**原创作者**：

- 指首次完成作品创作并具有著作权的的自然人或法人实体
- 需满足以下条件：
  1. 作品为独立创作成果（非抄袭或衍生作品）
  2. 创作过程包含独立思考与创新要素
  3. 未侵犯第三方合法权益
  4. 未违反国家或地方法律法规
  5. 遵守本标准
  6. 符合本标准
  7. 遵守本标准原则

**二创作者**：

- 在原创作品基础上的合法衍生创作者
- 需遵守：
  - 尊重原创作者的署名权
  - 不得歪曲原作品核心价值
  - 不侵犯原创作者和社区的合法权益
  - 不违反国家或地方法律法规
  - 遵守本标准
  - 遵守本标准
  - 符合本标准
  - 遵守、符合本标准原则

**基本原则**:
- 如您使用了这个标准,则必须要遵守
- 遵守法律法规!(条款 当地法律相关条款)
- 遵守《（XYLCS版）自由创作者创作精神宣言》(条款2.1)
- 无论何种情况,只要合法合规,任何创作者都具有创作自由,任何人无权剥夺创作自由(条款2.1 2.2)
- 无论何种情况,只要合法合规,任何类型的创作作品，都可以在本标准的适用范围内(条款2.1 1.2)
- 禁止一切形式的人身攻击(条款2.1 2.2)
- 无论何种情况,只要合法合规,任何创作者都具有创作平等对待权,任何人无权剥夺创作平等对待权(条款2.1 2.2)
- 禁止一切形式的违法违规违反此标准的创作社区政治管理行为(条款2.1 2.2)
- 创作性质应该自由开放(条款2.1)

### 1.2 适用范围

- 适用于所有采用本标准的创作项目
- 包括但不限于：
  - 开源软件项目
  - 跨媒体创作作品
  - 社区协作开发项目
  - 文化创作作品
  - 艺术创作作品
- 不适用情形：
  - 受法律特殊保护的文化作品
  - 违反国际公约或违反本标准、违法违规的创作内容

---

## 第二章 核心精神准则

### 2.1 基本原则体系

本标准基于《（XYLCS版）自由创作者创作精神宣言》.<br>
以下是精神宣言内容：<br>

- 1.创作应该是自由性的。
- 2.创作需要改正，此处指的改正，不只是创作过程中出现的错误，而是整体上的不良氛围或错误氛围、思想。
- 3.创作需要不断创新。
- 4.创作不限年龄，不限阶级，不限政治，人人平等。
- 5.创作应该保持协作。
- 6.创作应该保持永不停歇。

### 2.2 精神实施细则

- **自由性保障**：

  - 遵守《（XYLCS版）自由创作者创作精神宣言》
  - 遵守本标准
  - 符合本标准
  - 遵守与符合本标准原则
  - 作品符合精神宣言和本标准可称为自由创作作品
  - 在遵守本标准的前提下可自称为自由创作者
  - 禁止一切形式的人身攻击
  - 必须向外开放社区
  - 允许非商业用途且合法合规的的修改与分发
  - 禁止损害社区创作的种种行为
  - 禁止任何形式的不尊重人权
  - 禁止对社区的反馈进行恶意攻击
  - 禁止任何形式的不尊重社区
  - 禁止任何形式的对社区负面反馈进行封锁或封锁社区舆论
  - 不得进行任何不道德的行为
- **改进机制（建议）**：

  - 每定一段时间召开社区创作会议
  - 创作会议存档于记录Wiki

# 2.3 帮助其他社区/创作者的相关规定
 - 遵守《（XYLCS版）自由创作者创作精神宣言》
 - 遵守本标准
 - 符合本标准
 - 遵守与符合本标准原则
 - 其并不是创作社区和创作者的义务, 但是本标准建议您帮助, 以维护创作社区安宁稳定
 - 禁止任何形式的不尊重社区
 - 必须在符合本标准以及符合本标准原则的情况下帮助他人

---

## 第三章 权利与义务体系

### 3.1 基本权利矩阵


| 权利类型   | 原创作者权益                         | 二创作者权益                                |
| ---------- | ------------------------------------ | ------------------------------------------- |
| **署名权** | 必须在所有衍生作品中保留原始署名     | 需在显著位置标注二次创作声明                |
| **修改权** | 可随时撤回授权（需提前30天书面通知） | 重大修改需获得原创作者书面同意              |
| **收益权** | 保留商业授权收益的80%以上            | 可获得衍生作品收益的30%-50%（具体比例协商） |
| **发展权** | 对项目发展方向拥有最终决定权         | 可提出发展建议但需获得多数成员支持          |

### 3.2 特殊权利条款

- **社区发展权**：

  - 必须将项目贡献者纳入治理架构
  - 需设立自由创作总监和执行部
  - 不得恶意利用权利进行任何不合法、不合规、不合标准的行为
  - 不得阻碍社区发展
  - 不得恶意进行任何不合法、不合规、不合标准的行为
  - 不得进行任何不道德的行为
- **抗审查权**：

  - 在符合当地法律的前提下，有权拒绝不合理的审查要求
  - 需在公开渠道进行公开声明
  - 抗审查权的行使需符合法律和社区规定、本标准
  - 需提前60天向监管机构提交申诉材料

---

## 第四章 创作管理规范

### 4.1 许可证兼容性协议


| 原许可证类型/社区条例 | 允许的衍生许可证类型     | 冲突解决方案        |
| ------------ | ------------------------ | ------------------- |
| GPL-3.0      | AGPL-3.0, LGPL-3.0       | 采用GPL较严格条款,但保留本标准基本原则  |
| MIT          | Apache 2.0, BSD-3-Clause | 保留MIT核心授权条款,保留本标准基本原则 |
| CC BY-SA 4.0 | CC BY-NC-SA 4.0          | 使用SA授权版本,保留本标准基本原则     |
| kkko全部社区条例 | --- | kkko全部社区条例都遵守大部分的本标准，并无违反本标准的内容。小部分没有覆盖本标准的，则本标准补充|
| 其他社区条例 | --- | 保留本标准基本原则 |

### 4.2 贡献者协议流程

1. **贡献者声明**：
   - 需明确以下内容：
     - 原创声明或正确合规的二次创作声明
     - 无利益冲突声明
     - 知识产权归属声明
     - 遵守符合本标准以及遵守符合本标准原则

---

## 第五章 创作声明标准

### 6.1 声明创作作品格式规范

可以选择任意语言编写创作作品声明文本

```XFCSTD
<作品名称> (创作起始年份) by <组织/个人名称> (成立年份-当前年份) 自由创作 in XFCSTD2.
<work name> (creation start year) by <organization/personal name> (foundation year-current year) freedom-create in XFCSTD2.
<作品名称> (创作起始年份) by <组织/个人名称> (成立年份-当前年份) 自由二次创作 in XFCSTD2.
<work name> (creation start year) by <organization/personal name> (foundation year-current year) freedom-secondary-create in XFCSTD2.
```

这样即可声明该作品为自由创作作品<br>

### 6.2 声明创作社区/组织格式规范

选择任意语言编写创作社区声明文本
```XFCSTD
<社区名/组织名> (成立年份-当前年份) 自由创作 in XFCSTD2.
<community name>  (foundation year-current year) freedom-create in XFCSTD2.
```

这样即可声明该社区为自由创作社区/组织<br>

### 6.3 声明
声明后，您则应该遵守和符合本标准以及本标准的原则.<br>
声明后, 您则有权拥有本标准的给予权利.<br>
声明后, 您/创作作品/创作社区/创作者/组织等拥有该声明的版权和自由创作权.<br>
声明后, 您/创作作品/创作社区/创作者/组织等性质均为自由创作.<br>


本标准最终解释权归XYLCS STUDIO所有，任何修改必须经过XYLCS自由创作总监与执行部审议通过。标准文档的完整大历史版本可通过XYLCS STUDIO官网存证平台查阅。<br>
XYLCS STUDIO 官方网站:xylcs-studio.github.io<br>
XFCSTD2.md原版存储位置:xylcs-studio.github.io/Docm/XFCSTD2.md<br>
XFCSTD2.md网页预览版本:xylcs-studio.github.io/Creator/XFCSTD.html
");
                    

                }
                else
                {
                    Console.WriteLine("无效输入，请输入1、2或3。");
                }
            }
        }
    }
}