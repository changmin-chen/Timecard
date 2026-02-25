# API 快速測試

將 `<host>` 替換為實際部署位址：

```bash
curl -X POST http://<host>/api/clock/in   # 打卡上班
curl -X POST http://<host>/api/clock/out  # 打卡下班
curl http://<host>/api/day/today           # 今天摘要
curl http://<host>/api/month/2026/2        # 本月報表
```
