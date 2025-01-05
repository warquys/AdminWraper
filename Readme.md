# Admin Wraper

Admin wraper is an minimalistic console wraping the SCP sl process.
Contrary to [SecretAdmin](https://github.com/Jesus-QC/SecretAdmin) this console do not output a lot of verbose. Any log emitted by scp sl is redirected to a log file push when the round end or the server close. No comamnd input is possible and only general status (Server start/Server end) is displayed. The main purpose of this console is to provide a wrapper for a daemon process.

## Commands

If I add more commands or plugins, they will be listed here:

Start the server on port,

```bash
AdminWrapper start <Port>
```

Help,

```bash
AdminWrapper --help
```

## Notes

You are free to compile this programmed for your use, modify it and take pieces of code, unless it is the entire class (i do not care for nasted class) you do not have the obligation to credit the source.