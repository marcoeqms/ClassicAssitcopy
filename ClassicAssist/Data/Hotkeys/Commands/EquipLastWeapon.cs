using ClassicAssist.Data.Macros.Commands;

namespace ClassicAssist.Data.Hotkeys.Commands
{
    [HotkeyCommand( Name = "Equip Last Weapon (Quick Weapon Switch)", Tooltip = "Requires Server Support" )]
    public class EquipLastWeapon : HotkeyCommand
    {
        public override void Execute()
        {
            ActionCommands.EquipLastWeapon();
        }
    }
}