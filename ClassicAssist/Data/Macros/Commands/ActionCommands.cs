﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assistant;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class ActionCommands
    {
        [CommandsDisplay( Category = "Actions", Description = "Attack mobile (parameter can be serial or alias)." )]
        public static void Attack( object obj )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            Engine.SendPacketToServer( new AttackRequest( serial ) );
        }

        [CommandsDisplay( Category = "Actions", Description = "Clear hands, \"left\", \"right\", or \"both\"" )]
        public static void ClearHands( string hand )
        {
            hand = hand.ToLower();
            List<Layer> unequipLayers = new List<Layer>();

            switch ( hand )
            {
                case "left":
                    unequipLayers.Add( Layer.OneHanded );
                    break;
                case "right":
                    unequipLayers.Add( Layer.TwoHanded );
                    break;
                case "both":
                    unequipLayers.Add( Layer.OneHanded );
                    unequipLayers.Add( Layer.TwoHanded );
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof( hand ) );
            }

            PlayerMobile player = Engine.Player;

            List<int> serials = unequipLayers.Select( unequipLayer => Engine.Player.GetLayer( unequipLayer ) )
                .Where( serial => serial != 0 ).ToList();

            foreach ( int serial in serials )
            {
                UOC.DragDropAsync( serial, 1, player.Backpack?.Serial ?? 0 ).Wait();
                Thread.Sleep( Options.CurrentOptions.ActionDelayMS );
            }
        }

        [CommandsDisplay( Category = "Actions",
            Description = "Single click object (parameter can be serial or alias)." )]
        public static void ClickObject( object obj )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            Engine.SendPacketToServer( new LookRequest( serial ) );
        }

        [CommandsDisplay( Category = "Actions",
            Description = "Move item to container (parameters can be serials or aliases)." )]
        public static void MoveItem( object item, object destination, int amount = -1 )
        {
            int itemSerial = AliasCommands.ResolveSerial( item );

            if ( itemSerial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            Item itemObj = Engine.Items.GetItem( itemSerial );

            if ( itemObj == null )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            if ( amount == -1 )
            {
                amount = itemObj.Count;
            }

            int containerSerial = AliasCommands.ResolveSerial( destination );

            if ( containerSerial == 0 )
            {
                //TODO
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            UOC.DragDropAsync( itemSerial, amount, containerSerial ).Wait();
        }

        [CommandsDisplay( Category = "Actions",
            Description = "Unmounts if mounted, or mounts if unmounted, will prompt for mount if no \"mount\" alias." )]
        public static void ToggleMounted()
        {
            PlayerMobile player = Engine.Player;

            if ( player == null )
            {
                return;
            }

            if ( player.IsMounted )
            {
                Engine.SendPacketToServer( new UseObject( player.Serial ) );
                return;
            }

            if ( !AliasCommands.FindAlias( "mount" ) )
            {
                int serial = UOC.GetTargeSerialAsync( Strings.Target_new_mount___, 10000 ).Result;

                if ( serial == -1 )
                {
                    UOC.SystemMessage( Strings.Invalid_mount___ );
                    return;
                }

                AliasCommands.SetAlias( "mount", serial );
            }

            int mountSerial = AliasCommands.GetAlias( "mount" );

            Engine.SendPacketToServer( new UseObject( mountSerial ) );
        }

        [CommandsDisplay( Category = "Actions", Description = "Feed a given alias or serial with graphic." )]
        public static void Feed( object obj, int graphic, int amount = 1, int hue = -1 )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            if ( Engine.Player?.Backpack == null )
            {
                UOC.SystemMessage( Strings.Error__Cannot_find_player_backpack );
                return;
            }

            Item foodItem =
                Engine.Player.Backpack?.Container.SelectEntity( i => i.ID == graphic && ( hue == -1 || i.Hue == hue ) );

            if ( foodItem == null )
            {
                UOC.SystemMessage( Strings.Cannot_find_item___ );
                return;
            }

            UOC.DragDropAsync( foodItem.Serial, amount, serial ).Wait();
        }

        public static void Rename( object obj, string name )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            UOC.RenameRequest( serial, name );
        }

        [CommandsDisplay( Category = "Actions",
            Description = "Display corpses and/or mobiles names (parameter \"mobiles\" or \"corpses\"." )]
        public static void ShowNames( string showType )
        {
            const int MAX_DISTANCE = 32;
            const int corpseType = 0x2006;

            ShowNamesType enumValue = Utility.GetEnumValueByName<ShowNamesType>( showType );

            switch ( enumValue )
            {
                case ShowNamesType.Mobiles:

                    Mobile[] mobiles = Engine.Mobiles.SelectEntities( m =>
                        UOMath.Distance( m.X, m.Y, Engine.Player.X, Engine.Player.Y ) < MAX_DISTANCE );

                    if ( mobiles == null )
                    {
                        return;
                    }

                    foreach ( Mobile mobile in mobiles )
                    {
                        Engine.SendPacketToServer( new LookRequest( mobile.Serial ) );
                    }

                    break;
                case ShowNamesType.Corpses:

                    Item[] corpses = Engine.Items.SelectEntities( i =>
                        UOMath.Distance( i.X, i.Y, Engine.Player.X, Engine.Player.Y ) < MAX_DISTANCE &&
                        i.ID == corpseType );

                    if ( corpses == null )
                    {
                        return;
                    }

                    foreach ( Item corpse in corpses )
                    {
                        Engine.SendPacketToServer( new LookRequest( corpse.Serial ) );
                    }

                    break;
            }
        }

        [CommandsDisplay( Category = "Actions",
            Description = "Equip a specific item into a given layer. Use object inspector to determine layer value." )]
        public static void EquipItem( object obj, object layer )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            Layer layerValue = Layer.Invalid;

            switch ( layer )
            {
                case string s:
                    layerValue = Utility.GetEnumValueByName<Layer>( s );
                    break;
                case int i:
                    layerValue = (Layer) i;
                    break;
                case Layer l:
                    layerValue = l;
                    break;
            }

            if ( layerValue == Layer.Invalid )
            {
                UOC.SystemMessage( Strings.Invalid_layer_value___ );
                return;
            }

            Item item = Engine.Items.GetItem( serial );

            if ( item == null )
            {
                UOC.SystemMessage( Strings.Cannot_find_item___ );
                return;
            }

            UOC.EquipItem( item, layerValue );
        }

        private enum ShowNamesType
        {
            Mobiles,
            Corpses
        }
    }
}