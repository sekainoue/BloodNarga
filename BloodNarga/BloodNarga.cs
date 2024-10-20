﻿using ImGuiNET;
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
        private bool _afterCleave = false;
        private bool _afterTurn = true;
        private int _previousAction;
        private int _frameCounter;

        private void ResetState()
        {
            _bloodNarga = null; _banbaro = null;
            _actionCounts = 0;
            _stepCounts = 20;
            _transparentMode = false;
            _allBloodNargaDies = false;
            _afterCleave = false; _afterTurn = false;
            _frameCounter = 0;
            Monster.EnableSpeedReset();
        }
        public void OnMonsterDeath(Monster monster)
        {
            if (Quest.CurrentQuestId == 333003 && monster.Type == MonsterType.Banbaro)
            {
                _banbaro = null;
            }

            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                if (Quest.CurrentQuestId == 333003)
                {
                    _allBloodNargaDies = true;
                    _actionCounts = 0;
                    _bloodNarga = null;
                    _afterCleave = false; _afterTurn = false;
                    _frameCounter = 0;
                }
            }
        }

        public void OnMonsterCreate(Monster monster)
        {
            _lastStage = (uint)Area.CurrentStage;

            if (Quest.CurrentQuestId == 333003) { }
            else { return; }

            if (monster.Type == MonsterType.Banbaro)
            {
                _banbaro = monster;
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

        public void OnQuestLeave(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestComplete(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestFail(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestReturn(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAbandon(int questId) { ResetState(); _inQuest = false; }
        
        public void OnQuestEnter(int questId) // different from Quest Accept
        { 
            _inQuest = true;
            // do not ResetState because OnMonsterCreate occurs before OnQuestEnter
            // _bloodNarga = null; _banbaro = null;
            _actionCounts = 0;
            _stepCounts = 20;
            _transparentMode = false;
            _allBloodNargaDies = false;
            _afterCleave = false; _afterTurn = false;
            _frameCounter = 0;
            Monster.EnableSpeedReset();
        }
        public void OnQuestAccept (int questId) { ResetState(); _inQuest = false; }
        public void OnQuestCancel(int questId) { ResetState(); _inQuest = false; }
        public void OnMonsterDestroy(Monster monster)
        {
            if (monster.Type == MonsterType.Banbaro)
            {
                _banbaro = null;
            }

            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                // watch out for using ResetState here, because if Narga decayed all values are reset
                _bloodNarga = null;
                _actionCounts = 0;
                _transparentMode = false;
                _allBloodNargaDies = false;
                _afterCleave = false; _afterTurn = false;
                _frameCounter = 0;
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
                        Gui.DisplayPopup("Eat in quest for maximum darkness", TimeSpan.FromMilliseconds(5000));
                    }
                    else if (_stepCounts == 18)
                    {
                        _bloodNarga.ForceAction(Actions.TRIPLE_BLADE_ATTACK_L_F);
                        _banbaro.Health = 0f;
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


            if (_bloodNarga is not null && _bloodNarga == monster)
            {
                if (_actionCounts > 0)
                {
                    if (_actionCounts == 12 || _actionCounts == 6)
                    {
                        _transparentMode = true;
                    }
                    if (_actionCounts == 9 || _actionCounts == 2)
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

                if (_afterTurn)
                {
                    if (_previousAction == Actions.TURN_QUICK)
                    {
                        _bloodNarga.Speed = 1f;
                        actionId = Actions.TRIPLE_BLADE_ATTACK_R_T;
                        _afterTurn = false;
                    }
                }


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
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.3f;
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
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.3f;
                }
                else
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame;
                }
            }

            if (currentActionId == Actions.TAIL_STRIKE)
            {
                _frameCounter++;
                if (_frameCounter % 400 != 0) return;
                Monster.DisableSpeedReset(); _bloodNarga.Speed = 2.5f;
                _bloodNarga.ForceAction(Actions.TURN_QUICK); _afterTurn = true;
            }


            if (currentActionId >= Actions.TAIL_SLALOM_FRONT_L && currentActionId <= Actions.TAIL_SLALOM_BACK_R)
            {
                if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame < 0.07)
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame * 0.07f;
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.8f;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.3)
                {
                    _bloodNarga.Speed = 1f;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.95)
                {
                    _bloodNarga.Speed = 2f;
                }
                else
                {
                    Monster.EnableSpeedReset();
                }
            }

            if (currentActionId >= Actions.D_TAIL_SLALOM_FRONT_L && currentActionId <= Actions.D_TAIL_SLALOM_BACK_R)
            {
                if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame < 0.07)
                {
                    _bloodNarga.AnimationFrame = _bloodNarga.MaxAnimationFrame * 0.07f;
                    Monster.DisableSpeedReset(); _bloodNarga.Speed = 1.8f;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.8)
                {
                    _bloodNarga.Speed = 1f;
                }
                else if (_bloodNarga.AnimationFrame / _bloodNarga.MaxAnimationFrame <= 0.95)
                {
                    _bloodNarga.Speed = 2f;
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