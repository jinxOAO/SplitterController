# SplitterController
此mod方案由rojindo提出，并专用于混带建设，目的是为了在尽可能确保环路不停顿的前提下，分析流过的物品的比例，决定是否插入新的物品，以控制环路的物品比例。

当且仅当原版四项分流器被设置为“2进口，2出口，且设置了一个优先入口、一个优先出口”时，四向分流器UI中会出现文本框，可以填写比例，从而按所写比例混入两个入口的材料，在确保主输入口流入不停顿的基础上，保证从主出口输出的“副入口物品”比“非副入口物品”的比例符合玩家填写的比例。具体逻辑如下。

![main image](./img1.png)

## 主要逻辑与使用
- 以下将设置的优先入口称为主入口，非优先入口称为副入口。出口同理。
- 玩家需要对主入口和副入口分别输入比例（例如a:b）。
- 每当主入口流入a个“非副入口输入的物品”时，副入口会恰好将b个物品插入主入口输入流的空隙中。所有输出混在一起从主出口输出。
- 如果主入口物品持续满速，没有空隙可以插入，副入口仍会强行将b个物品插入并从主出口输出（以保证主出口输出的物品比例与设定值相同），此时主入口的物品仍然会被接收，只是会被挤出到副出口输出。由于是挤出物品而非让主输入口等待插入，因此主输入口不会停顿。
- 若副输入口缺货，主输入口的物品会直接输送到主输出口。

## 特殊情况与说明
- 只有副输出口堵满时，主输入口才有可能停顿或堵住。
- 如果主入口的物品，已经混杂了“副入口物品”和“其他物品”，在计算副入口是否应该插入物品时，主入口中已经存在的“副入口物品”会被一同计算在a:b的比例b中，并不会因为主入口有b物品残留而导致最终输出的比例错误。
- 如果因为副入口缺货，导致主入口反复流入大量的“非副入口物品”，并不会导致比例记录时，“非副入口物品”的数量被无限制地记录，最多只会记录-4个物品失衡数：
  - 这避免了：副入口缺货时，主路环带虽然只有几个“非副入口物品”，但是反复在环路中循环被记录了成百上千次，当副入口供货恢复后，分流器为了平衡比例，会持续优先插入成百上千个“副入口物品”，将所有其他物品都挤出到副出口，从而可能导致堵带。
  - 不过这也会导致：当副入口缺货时间过长导致环路有大量其他物品时，可能需要多个循环才能将比例平衡回来。
- 以下情况时，四向分流器维持原版逻辑：
  - 四向分流器不满足“2进口，2出口，且设置了一个优先入口、一个优先出口”条件时；
  - a和b均被玩家设置成了0时；
  - 四向分流器被设置了输出物品过滤器时；
  - 四向分流器上方放置了箱子时。

## 其他说明
- mod前置依赖为DSPModSave，请确保该mod已安装。
- 作者认为，“传送带物品记录功能，并依据该规则控制分拣器工作”是混带环路更好的解决方案，似乎已有人正在致力于相关的mod制作。
