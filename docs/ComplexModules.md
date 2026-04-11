# 复杂模块简述

## 判分（`AnswerGrader`）

- **单选**：忽略大小写比较整串文本。
- **多选**：按分隔符拆分选项后排序比较，忽略顺序与大小写。
- **判断**：将常见真/假写法归一为「对 / 错」再比较。
- **简答**：将标准答案按分隔符拆成多个关键词，要求用户答案全部包含（演示版策略，可替换为语义相似度）。

## 随机组卷

- 在「当前筛选后的题库池」内使用 `Random` 打乱顺序后截取前 N 题；题量与时长由界面绑定字段控制。

## 倒计时与交卷

- `DispatcherTimer` 每秒递减 `ExamRemainingSeconds`；到 0 时通过 UI 调度器触发异步交卷，避免与计时器回调直接重入。
- 使用 `_submitGate`（`Interlocked`）防止用户「交卷」与自动交卷并发重复提交。

## AI DTO 与降级

- `IAiTutorService` / `IQuestionRecommendationService` / `IStudyPlanService` 均为可替换接口；当前为本地占位实现，记录日志并返回结构化/可读文本结果，便于后续替换为 HTTP 客户端。
