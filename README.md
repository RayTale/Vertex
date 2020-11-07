## Vertex是一个基于Orleans开发的分布式、最终一致性、事件溯源的跨平台框架，用于构建高性能、高吞吐、低延时、可扩展的分布式应用程序

[![license](https://img.shields.io/github/license/RayTale/Vertex)](https://github.com/RayTale/Vertex/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Vertex.Runtime.svg?style=flat)](https://www.nuget.org/profiles/uless)
[![Join the chat at https://gitter.im/RayTale/Ray](https://badges.gitter.im/RayTale/Ray.svg)](https://gitter.im/RayTale/Ray?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
---
* 框架采用Event sourcing来解决分布式事务问题，除了提供超高吞吐的分布式事务能力之外，还提供状态变化的详细事件日志，易于追踪溯源，在某些领域有着天然的优势。
  
* 基于. Net 5.0和Orleans开发，支持从单个本地服务器扩展到多服务器集群，提供高可用能力。
---

### 项目起源

Vertex是Ray框架的3.0版本，Ray诞生之初是为虚拟币交易引擎提供一个全内存、高吞吐、低延时、可追溯、分布式的事务框架。

*__虚拟币交易引擎的指标要求比股票交易系统高很多，有一些硬性指标要求：__*
  
* 极高吞吐：大量搬砖机器人在下单撤单，对系统的吞吐要求非常高。
  
* 极低延时：虚拟币价格波动非常大，用户和挂单机器人都需要低延时来保证灵敏度，不然很容易亏损。
  
* 稳定性：7*24小时开放交易，对系统的高可用和伸缩性要求非常高。
  
* 横向拓展：大量的新增交易对，需要能够随时进行横向扩容。
  
* 可追溯性：要求对每一次金额变化和交易都有日志可追溯
  
*__遇到的困难__* 

* 事务流程较长：安全校验，金额变化，订单生成，撮合交易，订单更新，金额更新，账单生成，K线生成，挂单深度更新，触发计划订单...
  
> 如果按照传统的解决方案，如果要满足上述要求，除了巨大的复杂性之外还需要巨大的硬件成本
  
经过一段时间的研究和试验之后，决定使用saga + event sourcing结合来进行业务开发，但传统的各种类似的框架都存在各种问题，特别是性能问题。所以我决定基于Orleans来开发一个通用框架，经过半年多优化改良，框架达到了交易引擎要求的各项指标，单交易对能达到5000/s的订单能力，一次订单的提交延时控制在10ms以下，多交易对的处理能力可以通过增加集群节点来提高。

### 核心功能

* 高性能分布式Actor
* 事件溯源
* 事件存储
* 事件分发与订阅
* 最终一致性编程模型
* 强一致性编程模型

  
