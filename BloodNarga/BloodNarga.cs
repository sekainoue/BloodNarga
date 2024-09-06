using ImGuiNET;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Actions;
using SharpPluginLoader.Core.Entities;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Networking;
using System.Numerics;

namespace BloodNarga
{
    public class BloodNarga : IPlugin
    {
        public string Name => "Blood Narga";
        public string Author => "seka";

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private int _actionCounts = 0;
        private int _stepCounts = 20;
        private bool _inQuest = false;
        private bool _transparentMode = false;
        private bool _allBloodNargaDies = false;
        private Monster? _bloodNarga = null;
        private uint _lastStage = 0;
        private Monster? _banbaro = null;
        private Monster? _bazel = null;
        private Monster? _dosJagras = null;
        
        
        public void OnQuestLeave(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestComplete(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestFail(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestReturn(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAbandon(int questId) { ResetState(); _inQuest = false; }
        private void ResetState()
        {
            _bloodNarga = null; _banbaro = null; _bazel = null; _dosJagras = null;
            _actionCounts = 0;
            _stepCounts = 20;
            _transparentMode = false;
            _allBloodNargaDies = false;
            Monster.EnableSpeedReset();
        }
        public void OnQuestEnter(int questId) // different from Quest Accept
        { 
            _inQuest = true;
            // do not ResetState because OnMonsterCreate occurs before OnQuestEnter
            _actionCounts = 0;
            _stepCounts = 20;
            _transparentMode = false;
            _allBloodNargaDies = false;
        }
        public void OnMonsterCreate(Monster monster)
        {
            _lastStage = (uint)Area.CurrentStage;

            if (Quest.CurrentQuestId == 333002 || Quest.CurrentQuestId == 333003) { }
            else { return; }

            if (monster.Type == MonsterType.Banbaro)
            {
                _banbaro = monster;
            }
            else if (monster.Type == MonsterType.Bazelgeuse)
            {
                _bazel = monster;
            }
            else if (monster.Type == MonsterType.GreatJagras)
            {
                _dosJagras = monster;
            }
            else if (monster.Type == MonsterType.Vespoid)
            {
                //skip
            }
            else
            {
                _bloodNarga = monster;
            }
        }
        public void OnQuestAccept (int questId) { ResetState(); _inQuest = false; }
        public void OnQuestCancel(int questId) { ResetState(); _inQuest = false; }
        public void OnMonsterDestroy(Monster monster)
        {
            if (monster.Type == MonsterType.Banbaro)
                return;

            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                // watch out for using ResetState here, because if Narga decayed all values are reset
                _bloodNarga = null;
                _actionCounts = 0;
                _transparentMode = false;
                _allBloodNargaDies = false;
            }
        }
        public void OnMonsterDeath(Monster monster)
        {
            if (Quest.CurrentQuestId == 333003 && monster.Type == MonsterType.Banbaro)
            {
                _banbaro = null;
            }

            if (monster.Type == MonsterType.Bazelgeuse) { _bazel = null; }
            if (monster.Type == MonsterType.GreatJagras) { _dosJagras = null; Monster.EnableSpeedReset(); }

            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                if (Quest.CurrentQuestId == 333002 || Quest.CurrentQuestId == 333003)
                {
                    _allBloodNargaDies = true;
                    _actionCounts = 0;
                    _bloodNarga = null;
                }
            }
        }
        public void OnMonsterEnrage(Monster monster)
        {
            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                _actionCounts = 15;
            }
        }
        public void OnMonsterUnenrage(Monster monster)
        {
            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                _actionCounts = 0;
            }
        }

        public void OnMonsterAction(Monster monster, ref int actionId)
        {
            var player = Player.MainPlayer;
            if (player is null) return;
            if (!_inQuest) return;
            if (Quest.CurrentQuestId == 333003 && monster.Type == MonsterType.Banbaro && _banbaro is not null)
            {
                if (_stepCounts > 0)
                {
                    if (_stepCounts == 19)
                    {
                        _banbaro.Teleport(new Vector3(-2722.2f, 445.2f, 17207.6f));
                        Gui.DisplayPopup("EAT IN QUEST FOR MAXIMUM DARKNESS", TimeSpan.FromMilliseconds(5000));
                    }
                    else if (_stepCounts == 18)
                    {
                        _bloodNarga.ForceAction(189); // TRIPLEBLADE
                        monster.Health = 0f;
                    }
                    else if (_stepCounts == 15)
                    {
                        _bloodNarga.ForceAction(113);
                    }
                    else if (_stepCounts == 13)
                    {
                        _bloodNarga.ForceAction(122);
                        _stepCounts = 1;
                    }
                    _stepCounts--;
                }
                else
                {
                    //continue
                }
            }

            if (Quest.CurrentQuestId == 333002)
            {
                if (monster.Type == MonsterType.Bazelgeuse && _bazel is not null)
                {
                    actionId = actionId switch
                    {
                        105 => 69, 107 => 70, //because of area recentering issue, Threat to TailScatter
                        106 or 108 => 87, //threatfly to FallPress
                        166 => 73, //FloorFall to BlastTackleStart
                        // cannot change dash because it will be stuck in a loop of never reaching its target
                        _ => actionId
                    };
                }
                if (monster.Type == MonsterType.GreatJagras && _dosJagras is not null)
                {
                    actionId = actionId switch
                    {
                        >= 25 and <= 31 => 40, // SlimSearch to MeatEatToFat, beware of PredatorEat targeting another Monster
                        241 => 111, // BellyBreak to FAT_BACK_REVERSE_PETIT_START
                        37 => 41, // PredatorEat to SlimToFat
                        187 => 60, 233 => 114, // Clagger to JumpAttack
                        171 => 53, 222 => 99, // Down to ScratchAttackL
                        202 => 269, 248 => 98, // FloorFall to MaxPunch or FatBodyPress
                        196 => 192, 197 => 60, 242 => 236, 243 => 114, //TrapPara to Para, TrapPit to Jump
                        _ => actionId // default case, keep the original actionId
                    };
                }
            }

            // if (monster.Variant == 33) { }

            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                if (_actionCounts > 0)
                {
                    if (_actionCounts == 12 || _actionCounts == 0 || _actionCounts == 4)
                    {
                        _transparentMode = true;
                    }
                    if (_actionCounts == 9 || _actionCounts == 6 || _actionCounts == 2)
                    {
                        _transparentMode = false;
                    }
                    if (_actionCounts == 1)
                    {
                        _actionCounts += 14;
                    }
                    _actionCounts--;
                }
                if (_actionCounts == 0)
                {
                    _transparentMode = false;
                }

                if (Quest.CurrentQuestId == 333002 || Quest.CurrentQuestId == 333003)
                {
                    if ((actionId >= 195 && actionId <= 207) || (actionId >= 214 && actionId <= 218))
                    {
                        _bloodNarga.CreateShell(0, 3, _bloodNarga.Position, player.Position); // from player to Narga
                        _bloodNarga.CreateShell(0, 8, player.Position, _bloodNarga.Position); // from Narga to player
                    }
                }

                if (actionId == 8 || (actionId >= 21 && actionId <= 26)) { actionId = 212; } // narga search to thorn grow
                if (actionId == 30) { actionId = 211; } // ThreatShort to DThornSkip
            }
        }

        public void OnUpdate(float deltaTime)
        {
            var player = Player.MainPlayer;
            if (player is null) return;

            if ((uint)Area.CurrentStage != _lastStage)
            {
                ResetState();
            }

            if (!_inQuest) return;

            if (_bloodNarga is null) return;

            if (Quest.CurrentQuestId == 333002)
            {
                if (_stepCounts > 0)
                {
                    if (_stepCounts >= 18)
                    {
                        if (_dosJagras is not null)
                        {
                            _dosJagras.Resize(3);
                            Monster.DisableSpeedReset();
                            _dosJagras.Speed = 0.7f;
                        }
                    }
                    if (_stepCounts == 17 || _stepCounts == 13 || _stepCounts == 9 || _stepCounts == 5)
                    {
                        _bloodNarga.CreateShell(0, 1, _bloodNarga.Position, new Vector3(11010.6f, 505f, -4111.6f));
                    }
                    if (_stepCounts == 15 || _stepCounts == 11 || _stepCounts == 7 || _stepCounts == 3)
                    {
                        _bloodNarga.CreateShell(0, 1, _bloodNarga.Position, new Vector3(16641.600f, 612.200f, -7964f));
                    }
                    if (_stepCounts == 1)
                    {
                        Gui.DisplayPopup("Karmic Whirlpools, Footprints Of An Ishvalda.", TimeSpan.FromMilliseconds(5000));
                    }
                    _stepCounts--;
                }
                else if (_stepCounts == 0)
                {
                    //continue;
                }
            }

            ref float transparency = ref _bloodNarga.GetRef<float>(0x314);
            transparency = Clamp(transparency, 0.05f, 1.0f);

            if (!_transparentMode)
            {
                transparency = Math.Min(transparency + 0.025f, 1f);
            }
            else
            {
                transparency -= 0.025f;
            }

            if (_allBloodNargaDies)
            {
                ResetState();
            }
        }
    }
}