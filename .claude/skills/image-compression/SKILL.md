---
name: image-compression
description: 使用参数化命令行工具压缩或转换已有的 PNG、JPEG、WEBP、TGA 和 BMP 图像。适用于用户明确要求缩小尺寸、降低体积、转换格式、量化 PNG 或批量处理图像时；所有尺寸、格式和质量约束必须来自当前任务，不使用内置业务类别或资源预设。
---

# 图像压缩

## 工具

入口：`tools/ImageCompression_Tool/cli.py`

运行前确认工具存在，并用 `--help` 检查当前参数。需要 Python 依赖时使用项目
`.venv`，不要修改系统 Python。

## 执行规则

1. 从任务中读取输入路径、输出路径、最大宽高、目标格式、质量和透明度要求。
2. 未指定会导致有损转换的参数时先询问，不自行套用资源类别或尺寸预设。
3. 默认写入独立输出目录；只有用户明确要求时才使用 `--in-place`。
4. 批量处理目录时按需要使用 `--recursive`。
5. 需要机器可读结果时使用 `--json --quiet`。
6. 完成后核对成功数、失败数、输出尺寸、格式和透明度。

## 参数

- `--max-width` / `--max-height`：按比例限制尺寸，`0` 表示不限制。
- `--format`：`same`、`JPEG`、`PNG`、`WEBP`、`TGA` 或 `BMP`。
- `--quality`：JPEG/WEBP 质量。
- `--png-lossless`：PNG 无损优化。
- `--png-quantize` / `--png-colors`：PNG 调色板量化。
- `--keep-exif`：保留 JPEG EXIF。
- `--suffix`：为输出文件名追加后缀。

## 边界

- 不内置角色、物品、界面、场景、特效或其它业务资源类别。
- 不保存示例图片或项目专属压缩参数。
- 不把有损处理伪装成无损处理。
- 不覆盖原文件，除非用户明确授权。
