using ImGuiNET;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Actions;
using SharpPluginLoader.Core.Entities;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Networking;
using System;
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
        private bool _afterCleave = false;
        private bool _afterStrike = false;
        private int _previousAction;
        private int _frameCounter;


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
            _afterCleave = false; _afterStrike = false;
            _frameCounter = 0;
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
            _afterCleave = false; _afterStrike = false;
            _frameCounter = 0;
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
        public void OnQuestAccept(int questId) { ResetState(); _inQuest = false; }
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
                _afterCleave = false; _afterStrike = false;
                _frameCounter = 0;
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
                    _afterCleave = false; _afterStrike = false;
                    _frameCounter = 0;
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
                if (_bloodNarga is null) return;
                if (_stepCounts > 0)
                {
                    if (_stepCounts == 19)
                    {
                        _banbaro.Teleport(new Vector3(634.08f, -71.82f, 22323.86f));
                        Gui.DisplayPopup("EAT IN QUEST FOR MAXIMUM DARKNESS", TimeSpan.FromMilliseconds(5000));
                    }
                    else if (_stepCounts == 18)
                    {
                        _bloodNarga.ForceAction(Actions.TRIPLE_BLADE_ATTACK_L_F);
                        monster.Health = 0f;
                    }
                    else if (_stepCounts == 14)
                    {
                        _bloodNarga.ForceAction(Actions.SUNBATH);
                    }
                    else if (_stepCounts <= 12)
                    {
                        _bloodNarga.ForceAction(Actions.EAT);
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
                        105 => 69,
                        107 => 70, //because of area recentering issue, Threat to TailScatter
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
                        187 => 60,
                        233 => 114, // Clagger to JumpAttack
                        171 => 53,
                        222 => 99, // Down to ScratchAttackL
                        202 => 269,
                        248 => 98, // FloorFall to MaxPunch or FatBodyPress
                        196 => 192,
                        197 => 60,
                        242 => 236,
                        243 => 114, //TrapPara to Para, TrapPit to Jump
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

                /* if (Quest.CurrentQuestId == 333002 || Quest.CurrentQuestId == 333003)
                {
                    if ((actionId >= Actions.TAIL_CLEAVE_L && actionId <= Actions.D_TAIL_SLALOM_BACK_R) || (actionId >= Actions.COUNTER_TAIL_ATTACK_L && actionId <= Actions.COUNTER_TAIL_DOUBLE_ATTACK_R))
                    {
                        // _bloodNarga.CreateShell(0, 3, _bloodNarga.Position, player.Position); 
                        _bloodNarga.CreateShell(0, 8, player.Position, _bloodNarga.Position); // to from
                    }
                } */

                if (actionId == Actions.WALK_SEARCH || (actionId >= Actions.SEARCH && actionId <= Actions.LOST_SEARCH)) { _bloodNarga.ForceAction(Actions.GROW_TAIL_THORN); }
                if (actionId == Actions.SHORT_THREAT) { _bloodNarga.ForceAction(Actions.D_SKIP_THORN_MIDDLE_RANGE); } // ThreatShort to DThornSkip

                if (actionId == Actions.TAIL_CLEAVE_L || actionId == Actions.TAIL_CLEAVE_R)
                {
                    _afterCleave = true;
                }
                else if (_afterCleave)
                {
                    switch (_previousAction)
                    {
                        case Actions.TAIL_CLEAVE_L:
                            actionId = Actions.TAIL_SLALOM_FRONT_R;
                            break;
                        case Actions.TAIL_SLALOM_FRONT_R:
                            _afterCleave = false;
                            break;
                        case Actions.TAIL_CLEAVE_R:
                            actionId = Actions.TAIL_SLALOM_FRONT_L;
                            break;
                        case Actions.TAIL_SLALOM_FRONT_L:
                            _afterCleave = false;
                            break;
                    }
                }

                if (actionId == Actions.TAIL_STRIKE)
                { _afterStrike = true; }
                else { _afterStrike = false; }

                _frameCounter = 0;
                _previousAction = actionId;
                Monster.EnableSpeedReset();
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

            /*if (Quest.CurrentQuestId == 333002)
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
            }*/

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


            var currentActionId = _bloodNarga?.ActionController.CurrentAction.ActionId;
            if (_bloodNarga is null) return;
            if (currentActionId == Actions.TAIL_CLEAVE_L)
            {
                if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame < 0.5)
                {
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.6f;
                }
                else
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame;
                }
            }

            if (currentActionId == Actions.TAIL_CLEAVE_R)
            {
                if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame < 0.5)
                {
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.6f;
                }
                else
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame;
                }
            }

            if (_afterStrike == true)
            {
                _frameCounter++;
                if (_frameCounter % 400 != 0) return;
                _bloodNarga.ForceAction(Actions.TAIL_SLALOM_BACK_L);
            }


            if (currentActionId == Actions.TAIL_SLALOM_FRONT_L || currentActionId == Actions.TAIL_SLALOM_FRONT_R
                || currentActionId == Actions.TAIL_SLALOM_BACK_L || currentActionId == Actions.TAIL_SLALOM_BACK_R)
            {
                if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame < 0.1)
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame * 0.13f;
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 4;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.3)
                {
                    _bloodNarga.Speed = 1f;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.95)
                {
                    _bloodNarga.Speed = 3f;
                }
                else
                {
                    Monster.EnableSpeedReset();
                }
            }


            // CurrentAction is a property of type ActionInfo&, and it has a getter({ get; }) which means it can only be read, not set directly.

            // The & symbol here suggests that CurrentAction returns a reference to an ActionInfo object rather than a copy of it.

            // ActionId is a field of ActionInfo, holding the value for CurrentAction



            if (_allBloodNargaDies)
            {
                ResetState();
            }
        }
    }
}