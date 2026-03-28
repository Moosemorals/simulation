# Remote devcontainer on tayet (Podman + .NET 10)

This repository now contains a Podman-compatible devcontainer definition in `.devcontainer/devcontainer.json` and `.devcontainer/Containerfile`.

## 1. One-time setup on tayet

Connect to `tayet` and make sure Podman socket activation is enabled for your user:

```bash
ssh tayet
loginctl enable-linger "$USER"
systemctl --user enable --now podman.socket
systemctl --user status podman.socket --no-pager
podman --version
```

## 2. Connect to tayet in VS Code

1. Use **Remote-SSH: Connect to Host...** and choose `tayet`.
2. Open this repository folder on the remote host.
3. Run **Dev Containers: Reopen in Container**.

The devcontainer build will use:

- Base image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Non-root user: `vscode`
- Post-create restore: `dotnet restore uk.osric.sim.slnx`

## 3. Verify inside the container

After attach:

```bash
dotnet --info
dotnet build uk.osric.sim.slnx
```

## Notes

- The workspace setting `dev.containers.dockerPath` is set to `podman` so Dev Containers uses Podman on remote hosts.
- If the socket is not available after reboot, reconnect to `tayet` and run:

```bash
systemctl --user restart podman.socket
```
