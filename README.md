# 甜糖星愿自动签到、采集工具

[![Docker Hub Pulls](https://img.shields.io/docker/pulls/boris1993/tiantang-auto-harvest)](https://hub.docker.com/r/boris1993/tiantang-auto-harvest)
[![Build and publish the image](https://github.com/boris1993/tiantang-auto-harvest/actions/workflows/build-image.yml/badge.svg)](https://hub.docker.com/r/boris1993/tiantang-auto-harvest)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fboris1993%2Ftiantang-auto-harvest.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fboris1993%2Ftiantang-auto-harvest?ref=badge_shield)

每日自动签到，自动收取推广星愿和设备星愿。

目前功能：

- [x] 每日签到
- [x] 多推送平台支持 - Server酱、Bark、钉钉机器人
- [x] 自动收取推广星愿和设备星愿
- [x] 自动激活电费卡
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
      # 启用调试日志，如果没有问题的话不需要打开这个开关
      # - ASPNETCORE_ENVIRONMENT=Development
    ports:
      # 管理页面端口
      - "8080:80"
```

或者可以使用如下`Docker`命令启动：

```bash
docker run \
    --restart always \
    --name tiantang-auto-harvest \
    -p 8080:80 \
    -v /volume2/docker-data/tiantang-auto-harvest:/app/data \
    -e TZ=Asia/Shanghai \
    # 启用调试日志，如果没有问题的话不需要打开这个开关
    # -e ASPNETCORE_ENVIRONMENT=Development
    -d \
    boris1993/tiantang-auto-harvest:latest
```

如果从Docker Hub拉取镜像过慢，那么你也可以使用`registry.cn-hangzhou.aliyuncs.com/boris1993/tiantang-auto-harvest:latest`来从阿里云拉取。

再启动后，需要访问容器的端口（如上例中的`8080`）进入配置页面。在配置页面中你需要通过手机验证码登录。

如果有需要，那么可以填写`Server酱`和`Bark`的token，配置后即可收到通知。
目前通知内容包括：

- 每日收取的星愿数

# `arm v7`平台的用户需要注意

对于`arm v7`用户，如果你用上述命令无法启动，具体表现为容器反复重启，那么请删掉容器，然后执行如下命令以进入容器的`bash` shell：
```shell
docker run -it --rm --entrypoint /bin/bash boris1993/tiantang-auto-harvest:latest
```
然后在容器的shell内执行 `dotnet tiantang-auto-harvest.dll`，
如果出现错误信息 `Aborted (core dumped)`，那么请在上述启动命令的参数中加上`--privileged`。

# 推广码

如果你觉得这个工具好用，那么可不可以填一下我的邀请码`804744`，互惠互利？

![赞赏码](https://sat02pap001files.storage.live.com/y4mc9DXtRErXTWqB4T-e4MbNjh8grVux4vhbiUog6R_WOAWuI-pC2YbUxXi4-r5b-EaskCfAmnq7jLniVtelO423EbVYODuQX24u_QGlCzTj2yiiu1gUhCpc1bAH5srf2Tm5uC3eqESMz9ziyfkQKAUOhdXNNLTsvnWDm5rgBXjHM5eTyp1A3bcnXKHBRtdAFax?width=256&height=256&cropmode=none)

# 许可协议

该项目遵照[MIT License](LICENSE)开放源代码。
