using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessCommandQueue {

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentQueue<QueuedCommand> _commands = new ConcurrentQueue<QueuedCommand>();
    private int _executedCommands;

    public int PendingCount {
      get { return _commands.Count; }
    }

    public int ExecutedCommands {
      get { return _executedCommands; }
    }

    public AiHarnessResponse Run(string command, Func<object> action) {
      string commandId = command + "-" + Guid.NewGuid().ToString("N");
      var queuedCommand = new QueuedCommand(command, commandId, action);
      _commands.Enqueue(queuedCommand);

      if (!queuedCommand.Wait(DefaultTimeout)) {
        return AiHarnessResponse.Failure(command, commandId, "Timed out waiting for Timberborn to execute the command.");
      }

      if (queuedCommand.Error != null) {
        return AiHarnessResponse.Failure(command, commandId, queuedCommand.Error);
      }

      return AiHarnessResponse.Success(command, commandId, queuedCommand.Data);
    }

    public void Drain(int maxCommands) {
      int drained = 0;
      while (drained < maxCommands && _commands.TryDequeue(out QueuedCommand queuedCommand)) {
        queuedCommand.Execute();
        Interlocked.Increment(ref _executedCommands);
        drained++;
      }
    }

    private sealed class QueuedCommand {

      private readonly Func<object> _action;
      private readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);

      public QueuedCommand(string command, string commandId, Func<object> action) {
        Command = command;
        CommandId = commandId;
        _action = action;
      }

      public string Command { get; }
      public string CommandId { get; }
      public object Data { get; private set; } = new object();
      public string? Error { get; private set; }

      public bool Wait(TimeSpan timeout) {
        return _completed.Wait(timeout);
      }

      public void Execute() {
        try {
          Data = _action();
        } catch (Exception exception) {
          Error = exception.Message;
          Debug.LogException(exception);
        } finally {
          _completed.Set();
          _completed.Dispose();
        }
      }

    }

  }
}
