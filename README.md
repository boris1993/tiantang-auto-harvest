# 甜糖星愿自动签到、采集工具

[![Docker Hub Pulls](https://img.shields.io/docker/pulls/boris1993/tiantang-auto-harvest)](https://hub.docker.com/r/boris1993/tiantang-auto-harvest)
[![Build and publish the image](https://github.com/boris1993/tiantang-auto-harvest/actions/workflows/build-image.yml/badge.svg)](https://hub.docker.com/r/boris1993/tiantang-auto-harvest)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fboris1993%2Ftiantang-auto-harvest.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fboris1993%2Ftiantang-auto-harvest?ref=badge_shield)

每日自动签到，自动收取推广星愿和设备星愿。

目前功能：

- [x] 每日签到
- [x] 多推送平台支持 - Server酱、Bark
- [x] 自动收取推广星愿和设备星愿
- [x] 自动激活电费卡
- [ ] 自动激活星愿加成卡
- [ ] 自动提现
- [x] token过期后自动刷新登录


# 使用

如果你使用`Docker Compose`或`Portainer`管理Docker容器，那么可以使用该`docker-compose.yml`：

```yaml
---
version: '3'

services:
  tiantang-auto-harvest:
    image: boris1993/tiantang-auto-harvest:latest
    container_name: tiantang-auto-harvest
    restart: always
    volumes: 
      # 备份数据库文件
      - /volume2/docker-data/tiantang-auto-harvest:/app/data
    environment:
      - TZ=Asia/Shanghai
    ports:
      # 管理页面端口
      - 8080:80
```

或者可以使用如下`Docker`命令启动：

```bash
docker run \
    --restart always \
    --name tiantang-auto-harvest \
    -p 8080:80 \
    -v /volume2/docker-data/tiantang-auto-harvest:/app/data \
    -e TZ=Asia/Shanghai \
    boris1993/tiantang-auto-harvest:latest
```

再启动后，需要访问容器的端口（如上例中的`8080`）进入配置页面。在配置页面中你需要通过手机验证码登录。

如果有需要，那么可以填写`Server酱`和`Bark`的token，配置后即可收到通知。
目前通知内容包括：

- 每日收取的星愿数

# 许可协议

该项目遵照[MIT License](LICENSE)开放源代码。
