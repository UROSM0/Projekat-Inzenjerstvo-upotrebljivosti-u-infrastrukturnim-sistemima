using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using NetworkService.Helpers;
using NetworkService.Model;
using NetworkService.Services;

namespace NetworkService.ViewModel
{
    public class TerminalViewModel : BindableBase
    {
        public ObservableCollection<string> Output { get; } = new ObservableCollection<string>();
        private string _input = "";
        public string Input
        {
            get => _input;
            set => SetProperty(ref _input, value);
        }

        public ICommand SubmitCommand { get; }
        public ICommand CloseCommand { get; }

        private readonly MainWindowViewModel _main;
        private readonly IWindowService _windows;

        
        private readonly List<string> _history = new List<string>();
        private int _histIndex = -1;

        
        private readonly Stack<IUndoable> _undo = new Stack<IUndoable>();
        private readonly Stack<IUndoable> _redo = new Stack<IUndoable>();

        public TerminalViewModel(MainWindowViewModel main, IWindowService windows)
        {
            _main = main;
            _windows = windows;

            SubmitCommand = new MyICommand(Submit);
            CloseCommand = new MyICommand(() => _windows.CloseTerminal());

            PrintWelcome();
        }

        private void PrintWelcome()
        {
            Output.Add("NetworkService Terminal — type 'help' for commands");
        }

        private void Submit()
        {
            var cmd = (Input ?? "").Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            Output.Add($"$ {cmd}");
            _history.Add(cmd);
            _histIndex = _history.Count;

            Input = string.Empty;

            try
            {
                Execute(cmd);
            }
            catch (Exception ex)
            {
                Output.Add($"ERR: {ex.Message}");
            }
        }

        private void Execute(string cmdLine)
        {
            var (verb, args) = SplitVerbArgs(cmdLine);

            switch (verb.ToLowerInvariant())
            {
                case "help": PrintHelp(); break;
                case "clear": Output.Clear(); break;
                case "exit": _windows.CloseTerminal(); break;

                case "nav": CmdNav(args); break;

                case "list": CmdList(args); break;

                case "add": CmdAdd(args); break;
                case "delete": CmdDelete(args); break;

                case "place": CmdPlace(args); break;
                case "clear-slot": CmdClearSlot(args); break;

                case "connect": CmdConnect(args); break;
                case "disconnect": CmdDisconnect(args); break;

                case "undo": CmdUndo(); break;
                case "redo": CmdRedo(); break;

                default:
                    Output.Add("Unknown command. Type 'help'.");
                    break;
            }
        }

        private void PrintHelp()
        {
            Output.Add("Commands:");
            Output.Add("  help");
            Output.Add("  nav entities|display|graph");
            Output.Add("  list entities | list slots");
            Output.Add("  add entity id=<int> name=\"<text>\" type=RTD|TermoSprega");
            Output.Add("  delete entity id=<int>");
            Output.Add("  place id=<int> slot=<0-11>");
            Output.Add("  clear-slot slot=<0-11>");
            Output.Add("  connect a=<id> b=<id>");
            Output.Add("  disconnect a=<id> b=<id>");
            Output.Add("  undo | redo");
            Output.Add("  clear");
            Output.Add("  exit");
        }

        #region Commands

        private void CmdNav(Dictionary<string, string> args)
        {
            var target = GetPositional(args, 0);
            var nav = _main.NavCommand as ICommand;
            switch (target?.ToLowerInvariant())
            {
                case "entities": nav?.Execute("entities"); Output.Add("OK: navigated to Entities."); break;
                case "display": nav?.Execute("display"); Output.Add("OK: navigated to Display."); break;
                case "graph": nav?.Execute("graph"); Output.Add("OK: navigated to Graph."); break;
                default: Output.Add("Usage: nav entities|display|graph"); break;
            }
        }

        private void CmdList(Dictionary<string, string> args)
        {
            var what = GetPositional(args, 0);
            if (string.Equals(what, "entities", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var e in _main.Entities.OrderBy(e => e.Id))
                {
                    var lastTxt = e.LastValue.HasValue ? e.LastValue.Value.ToString("F2", CultureInfo.InvariantCulture) : "—";
                    Output.Add($"  id={e.Id}  name={e.Name}  type={e.Type?.Name}  last={lastTxt}");
                }
                if (_main.Entities.Count == 0) Output.Add("  (no entities)");
                return;
            }

            if (string.Equals(what, "slots", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var s in _main.DisplayVM.Slots.OrderBy(s => s.Index))
                {
                    var occ = s.Occupant != null ? $"id={s.Occupant.Id} ({s.Occupant.Name})" : "empty";
                    Output.Add($"  slot={s.Index}  {occ}");
                }
                return;
            }

            Output.Add("Usage: list entities | list slots");
        }

        private void CmdAdd(Dictionary<string, string> args)
        {
            var noun = GetPositional(args, 0);
            if (!string.Equals(noun, "entity", StringComparison.OrdinalIgnoreCase))
            {
                Output.Add("Usage: add entity id=<int> name=\"<text>\" type=RTD|TermoSprega");
                return;
            }

            if (!TryGetInt(args, "id", out var id)) { Output.Add("Missing: id=<int>"); return; }
            if (!args.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name)) { Output.Add("Missing: name=\"<text>\""); return; }
            if (!args.TryGetValue("type", out var type)) { Output.Add("Missing: type=RTD|TermoSprega"); return; }

            if (_main.Entities.Any(e => e.Id == id)) { Output.Add($"Entity with id={id} already exists."); return; }

            var sensorType = _main.SensorTypes.FirstOrDefault(t => string.Equals(t.Name, type, StringComparison.OrdinalIgnoreCase));
            if (sensorType == null) { Output.Add("Unknown type. Use RTD or TermoSprega."); return; }

            var entity = new ReactorTemp { Id = id, Name = name, Type = sensorType };
            var action = new LambdaAction(
                doAct: () => _main.Entities.Add(entity),
                undoAct: () => _main.Entities.Remove(entity),
                desc: $"add entity {id}");

            Apply(action);
            Output.Add($"OK: entity id={id} added.");
        }

        private void CmdDelete(Dictionary<string, string> args)
        {
            var noun = GetPositional(args, 0);
            if (!string.Equals(noun, "entity", StringComparison.OrdinalIgnoreCase))
            {
                Output.Add("Usage: delete entity id=<int>");
                return;
            }

            if (!TryGetInt(args, "id", out var id)) { Output.Add("Missing: id=<int>"); return; }
            var e = _main.Entities.FirstOrDefault(x => x.Id == id);
            if (e == null) { Output.Add($"Entity id={id} not found."); return; }

            
            var slot = _main.DisplayVM.Slots.FirstOrDefault(s => s.Occupant?.Id == id);

            var action = new LambdaAction(
                doAct: () =>
                {
                    if (slot != null) _main.DisplayVM.RemoveFromSlot(slot.Index);
                    _main.Entities.Remove(e);
                },
                undoAct: () =>
                {
                    _main.Entities.Add(e);
                    if (slot != null) _main.DisplayVM.PlaceInSlot(slot.Index, e);
                },
                desc: $"delete entity {id}");

            Apply(action);
            Output.Add($"OK: entity id={id} deleted.");
        }

        private void CmdPlace(Dictionary<string, string> args)
        {
            if (!TryGetInt(args, "id", out var id)) { Output.Add("Missing: id=<int>"); return; }
            if (!TryGetInt(args, "slot", out var slotIndex)) { Output.Add("Missing: slot=<0-11>"); return; }
            if (slotIndex < 0 || slotIndex >= _main.DisplayVM.Slots.Count) { Output.Add("Slot out of range."); return; }

            var e = _main.Entities.FirstOrDefault(x => x.Id == id);
            if (e == null) { Output.Add($"Entity id={id} not found."); return; }

            var oldSlot = _main.DisplayVM.Slots.FirstOrDefault(s => s.Occupant?.Id == id);
            var replaced = _main.DisplayVM.Slots[slotIndex].Occupant;

            var action = new LambdaAction(
                doAct: () => _main.DisplayVM.PlaceInSlot(slotIndex, e),
                undoAct: () =>
                {
                   
                    if (replaced != null)
                        _main.DisplayVM.PlaceInSlot(slotIndex, replaced);
                    else
                        _main.DisplayVM.RemoveFromSlot(slotIndex);

                    if (oldSlot != null)
                        _main.DisplayVM.PlaceInSlot(oldSlot.Index, e);
                    else
                        _main.DisplayVM.RemoveFromSlot(_main.DisplayVM.Slots.First(s => s.Occupant?.Id == id)?.Index ?? slotIndex);
                },
                desc: $"place entity {id} -> slot {slotIndex}");

            Apply(action);
            Output.Add($"OK: placed id={id} at slot={slotIndex}.");
        }

        private void CmdClearSlot(Dictionary<string, string> args)
        {
            if (!TryGetInt(args, "slot", out var slotIndex)) { Output.Add("Missing: slot=<0-11>"); return; }
            if (slotIndex < 0 || slotIndex >= _main.DisplayVM.Slots.Count) { Output.Add("Slot out of range."); return; }

            var prev = _main.DisplayVM.Slots[slotIndex].Occupant;

            var action = new LambdaAction(
                doAct: () => _main.DisplayVM.RemoveFromSlot(slotIndex),
                undoAct: () => { if (prev != null) _main.DisplayVM.PlaceInSlot(slotIndex, prev); },
                desc: $"clear-slot {slotIndex}");

            Apply(action);
            Output.Add($"OK: cleared slot={slotIndex}.");
        }

        private void CmdConnect(Dictionary<string, string> args)
        {
            if (!TryGetInt(args, "a", out var ida) || !TryGetInt(args, "b", out var idb))
            { Output.Add("Usage: connect a=<id> b=<id>"); return; }

            var sa = _main.DisplayVM.Slots.FirstOrDefault(s => s.Occupant?.Id == ida);
            var sb = _main.DisplayVM.Slots.FirstOrDefault(s => s.Occupant?.Id == idb);
            if (sa == null || sb == null) { Output.Add("Both entities must be placed on the grid."); return; }

            
            var exists = _main.DisplayVM.Connections.Any(c =>
            {
                var A = c.A?.Occupant?.Id; var B = c.B?.Occupant?.Id;
                return (A == ida && B == idb) || (A == idb && B == ida);
            });
            if (exists) { Output.Add("Connection already exists."); return; }

            var conn = new ConnectionVM(sa, sb);

            var action = new LambdaAction(
                doAct: () => _main.DisplayVM.Connections.Add(conn),
                undoAct: () => _main.DisplayVM.Connections.Remove(conn),
                desc: $"connect {ida}-{idb}");

            Apply(action);
            Output.Add($"OK: connected {ida} ↔ {idb}.");
        }

        private void CmdDisconnect(Dictionary<string, string> args)
        {
            if (!TryGetInt(args, "a", out var ida) || !TryGetInt(args, "b", out var idb))
            { Output.Add("Usage: disconnect a=<id> b=<id>"); return; }

            var conn = _main.DisplayVM.Connections.FirstOrDefault(c =>
            {
                var A = c.A?.Occupant?.Id; var B = c.B?.Occupant?.Id;
                return (A == ida && B == idb) || (A == idb && B == ida);
            });

            if (conn == null) { Output.Add("Connection not found."); return; }

            var action = new LambdaAction(
                doAct: () => _main.DisplayVM.Connections.Remove(conn),
                undoAct: () => _main.DisplayVM.Connections.Add(conn),
                desc: $"disconnect {ida}-{idb}");

            Apply(action);
            Output.Add($"OK: disconnected {ida} ↔ {idb}.");
        }

        private void CmdUndo()
        {
            if (_undo.Count == 0) { Output.Add("Nothing to undo."); return; }
            var act = _undo.Pop();
            act.Undo();
            _redo.Push(act);
            Output.Add($"Undone: {act.Description}");
        }

        private void CmdRedo()
        {
            if (_redo.Count == 0) { Output.Add("Nothing to redo."); return; }
            var act = _redo.Pop();
            act.Do();
            _undo.Push(act);
            Output.Add($"Redone: {act.Description}");
        }

        private void Apply(IUndoable action)
        {
            action.Do();
            _undo.Push(action);
            _redo.Clear();
        }

        #endregion

        #region Parsing helpers

        private static (string verb, Dictionary<string, string> args) SplitVerbArgs(string line)
        {
            var parts = Tokenize(line);
            if (parts.Count == 0) return ("", new Dictionary<string, string>());
            var verb = parts[0];
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            
            for (int i = 1, pos = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                var eq = p.IndexOf('=');
                if (eq > 0)
                {
                    var k = p.Substring(0, eq).Trim();
                    var v = p.Substring(eq + 1).Trim().Trim('"');
                    dict[k] = v;
                }
                else
                {
                    dict[$"_{pos++}"] = p.Trim().Trim('"');
                }
            }

            return (verb, dict);
        }

        private static List<string> Tokenize(string line)
        {
            var res = new List<string>();
            bool inQ = false;
            var cur = new List<char>();
            foreach (var ch in line)
            {
                if (ch == '"') { inQ = !inQ; continue; }
                if (char.IsWhiteSpace(ch) && !inQ)
                {
                    if (cur.Count > 0) { res.Add(new string(cur.ToArray())); cur.Clear(); }
                }
                else cur.Add(ch);
            }
            if (cur.Count > 0) res.Add(new string(cur.ToArray()));
            return res;
        }

        private static string GetPositional(Dictionary<string, string> args, int index) =>
            args.TryGetValue($"_{index}", out var v) ? v : null;

        private static bool TryGetInt(Dictionary<string, string> args, string key, out int value)
        {
            value = 0;
            if (!args.TryGetValue(key, out var s)) return false;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private interface IUndoable { void Do(); void Undo(); string Description { get; } }

        private class LambdaAction : IUndoable
        {
            private readonly Action _do, _undo;
            public string Description { get; }
            public LambdaAction(Action doAct, Action undoAct, string desc)
            {
                _do = doAct; _undo = undoAct; Description = desc;
            }
            public void Do() => _do();
            public void Undo() => _undo();
        }

        #endregion
    }
}
