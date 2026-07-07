using System;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MPSpecCamLocker
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            InformationManager.DisplayMessage(new InformationMessage("MPSpecCamLocker Loaded", Colors.White));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
        }

        public override async void OnInitialState()
        {
            base.OnInitialState();
            try
            {
                var networkMainType = Type.GetType("TaleWorlds.MountAndBlade.NetworkMain, TaleWorlds.MountAndBlade");
                if (networkMainType == null) return;

                var gameClientProperty = networkMainType.GetProperty("GameClient", BindingFlags.Public | BindingFlags.Static);
                if (gameClientProperty == null) return;

                object gameClient = null;
                while (gameClient == null)
                {
                    gameClient = gameClientProperty.GetValue(null);
                    await Task.Delay(1);
                }

                FieldInfo field = gameClient.GetType().GetField("_loadedUnofficialModules", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null) return;

                object moduleListObj;
                while ((moduleListObj = field.GetValue(gameClient)) == null)
                {
                    await Task.Delay(1);
                }

                var moduleList = moduleListObj as System.Collections.IList;
                if (moduleList == null) return;

                var newList = Activator.CreateInstance(moduleListObj.GetType());
                var addMethod = newList.GetType().GetMethod("Add");

                foreach (var module in moduleList)
                {
                    var idProperty = module.GetType().GetProperty("Id");
                    if (idProperty != null)
                    {
                        var id = idProperty.GetValue(module) as string;
                        if (id != "MPSpecCamLocker")
                        {
                            addMethod.Invoke(newList, new[] { module });
                        }
                    }
                }

                field.SetValue(gameClient, newList);
                InformationManager.DisplayMessage(new InformationMessage("MPSpecCamLocker ready. Developed by Vader", Colors.Yellow));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"MPSpecCamLocker Error: {ex.Message}", Colors.Red));
            }
        }
    }
}