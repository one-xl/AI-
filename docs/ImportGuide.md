# 题库导入指南

本指南说明如何使用题库导入功能批量导入题目。

## 1. 功能概述

- 支持从 JSON 文件批量导入题目
- 自动验证数据格式
- 提供详细的导入结果报告
- 导入成功后自动刷新题库列表

## 2. JSON 数据格式

### 2.1 完整示例

```json
[
  {
    "type": "SingleChoice",
    "difficulty": "Easy",
    "stem": "C# 中值类型与引用类型的关键区别是什么？",
    "standardAnswer": "A",
    "optionsJson": "[\"A. 值类型通常分配在栈上，引用类型的变量保存对象引用\",\"B. 引用类型不能为 null\",\"C. 值类型一定比引用类型更快\",\"D. 二者没有区别\"]",
    "knowledgeTags": "C#,基础,类型系统",
    "isEnabled": true
  },
  {
    "type": "TrueFalse",
    "difficulty": "Easy",
    "stem": "async/await 关键字会创建新的操作系统线程。",
    "standardAnswer": "错",
    "optionsJson": null,
    "knowledgeTags": "C#,异步",
    "isEnabled": true
  },
  {
    "type": "ShortAnswer",
    "difficulty": "Medium",
    "stem": "简述依赖注入（DI）在桌面应用中的两个好处。",
    "standardAnswer": "解耦;可测试",
    "optionsJson": null,
    "knowledgeTags": "架构,DI",
    "isEnabled": true
  }
]
```

### 2.2 字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `type` | string | 是 | 题型，有效值：`SingleChoice`、`MultipleChoice`、`TrueFalse`、`ShortAnswer` |
| `difficulty` | string | 是 | 难度，有效值：`Easy`、`Medium`、`Hard` |
| `stem` | string | 是 | 题干内容 |
| `standardAnswer` | string | 是 | 标准答案 |
| `optionsJson` | string | 否 | 选项 JSON 数组（客观题使用） |
| `knowledgeTags` | string | 否 | 知识点标签，逗号分隔 |
| `isEnabled` | boolean | 否 | 是否启用，默认 `true` |

### 2.3 题型说明

#### 单选题 (SingleChoice)
```json
{
  "type": "SingleChoice",
  "standardAnswer": "A",
  "optionsJson": "[\"A. 选项1\",\"B. 选项2\",\"C. 选项3\"]"
}
```

#### 多选题 (MultipleChoice)
```json
{
  "type": "MultipleChoice",
  "standardAnswer": "A,C",
  "optionsJson": "[\"A. 选项1\",\"B. 选项2\",\"C. 选项3\"]"
}
```

#### 判断题 (TrueFalse)
```json
{
  "type": "TrueFalse",
  "standardAnswer": "对"
}
```
标准答案可使用：`对`/`错`、`true`/`false`、`yes`/`no`、`是`/`否`

#### 简答题 (ShortAnswer)
```json
{
  "type": "ShortAnswer",
  "standardAnswer": "关键词1;关键词2"
}
```
标准答案使用分号或逗号分隔多个关键词，用户答案需包含所有关键词才判为正确。

### 2.4 OptionsJson 格式

OptionsJson 是一个 JSON 数组的字符串表示：

```json
"optionsJson": "[\"选项 A\",\"选项 B\",\"选项 C\"]"
```

注意：
- 必须是有效的 JSON 数组字符串
- 内部双引号需要转义为 `\"`
- 简答题和判断题可以设为 `null` 或省略

## 3. 使用步骤

1. 准备符合上述格式的 JSON 文件
2. 在应用程序的「题库管理」页面点击「导入题库」按钮
3. 选择准备好的 JSON 文件
4. 查看导入结果报告

## 4. 导入验证

导入时会自动验证：
- 必填字段是否存在
- 题型和难度值是否有效
- JSON 格式是否正确
- OptionsJson（如提供）是否为有效 JSON 数组

## 5. 错误处理

- 部分题目导入失败不会影响其他题目的导入
- 导入完成后会显示详细的错误信息
- 最多显示前 20 条错误，超过会提示剩余数量

## 6. 示例文件

项目中提供了完整的示例文件：`docs/sample-questions.json`
