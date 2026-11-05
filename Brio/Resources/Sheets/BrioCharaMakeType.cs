
//
// Some code in this file was generated and is from the Lumina.Excel project (https://github.com/NotAdam/Lumina.Excel)
// (Lumina.Excel.Sheets.CharaMakeType)
//

using Brio.Game.Actor.Appearance;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Sheets;

// Decode through the API14 SDK row instead of maintaining private column offsets.
// API14 inserts six columns into every customization menu; API13 offsets read the wrong data.
public static class BrioCharaMakeType
{
    public const int MenuCount = 28;
    public const int SubMenuParamCount = 100;
    public const int SubMenuGraphicCount = 10;
    public const int VoiceCount = 12;
    public const int FaceCount = 8;
    public const int FaceFeatureCount = 7;

    public static MenuCollection BuildMenus(ActorAppearance appearance)
    {
        var menus = new List<Menu>();

        var CharaMakeTypes = GameDataProvider.Instance.DataManager.GetExcelSheet<CharaMakeType>().
            Where(x => x.Gender == (sbyte)appearance.Customize.Gender && x.Race.RowId == (uint)appearance.Customize.Race).
            First();

        for(uint i = 0; i < CharaMakeTypes.CharaMakeStruct.Count; ++i)
        {
            var firstChar = CharaMakeTypes.CharaMakeStruct[(int)i];

            var title = firstChar.Menu.ValueNullable?.Text.ExtractText() ?? "Unknown";
            var menuType = (MenuType)firstChar.SubMenuType;
            var subMenuNum = firstChar.SubMenuNum;
            var subMenuMask = firstChar.SubMenuMask;
            var customizeIndex = (CustomizeIndex)firstChar.Customize;
            var initialValue = firstChar.InitVal;

            var subParams = new int[subMenuNum];
            var subGraphics = new byte[SubMenuGraphicCount];

            int[,] FacialFeatures = new int[FaceCount, FaceFeatureCount];

            for(int y = 0; y < FaceCount; ++y)
            {
                var faceOption = CharaMakeTypes.FacialFeatureOption[y];
                for(int x = 0; x < FaceFeatureCount; ++x)
                {
                    FacialFeatures[y, x] = x switch
                    {
                        0 => faceOption.Option1, 1 => faceOption.Option2, 2 => faceOption.Option3,
                        3 => faceOption.Option4, 4 => faceOption.Option5, 5 => faceOption.Option6,
                        6 => faceOption.Option7, _ => 0,
                    };
                }
            }

            for(int x = 0; x < subMenuNum; ++x)
            {
                if(x >= SubMenuParamCount)
                {
                    subParams[x] = 0;
                    continue;
                }

                subParams[x] = (int)firstChar.SubMenuParam[x];
            }

            menus.Add(new Menu(i, CharaMakeTypes.RowId, title,
                CharaMakeTypes.Race.IsValid ? (Races)CharaMakeTypes.Race.Value.RowId : 0,
                CharaMakeTypes.Tribe.IsValid ? (Tribes)CharaMakeTypes.Tribe.Value.RowId : 0,
                (Genders)CharaMakeTypes.Gender, menuType, subMenuMask, customizeIndex,
                initialValue, subParams, subGraphics, [.. CharaMakeTypes.VoiceStruct], FacialFeatures));


        }

        return new MenuCollection([.. menus]);
    }

    public class MenuCollection(Menu[] menus)
    {
        public Menu[] Menus { get; } = menus;

        public Menu? GetMenuForCustomize(CustomizeIndex index)
        {
            return Menus.FirstOrDefault(x => x.CustomizeIndex == index);
        }

        public Menu[] GetMenusForCustomize(CustomizeIndex index)
        {
            return Menus.Where(x => x.CustomizeIndex == index).ToArray();
        }

        public MenuType GetMenuTypeForCustomize(CustomizeIndex index)
        {
            return GetMenuForCustomize(index)?.Type ?? MenuType.Unknown;
        }
    }

    public record class Menu(uint MenuId,
        uint CharaMakeRow, string Title,
        Races Race, Tribes Tribe, Genders
        Gender, MenuType Type, uint MenuMask,
        CustomizeIndex CustomizeIndex, byte InitialValue,
        int[] SubParams, byte[] SubGraphics, byte[] Voices,
        int[,] FacialFeatures);

    public enum MenuType : byte
    {
        List = 0,
        ItemSelect = 1,
        Color = 2,
        Unknown3 = 3,
        MultiItemSelect = 4,
        Numerical = 5,
        Unknown,
    }
}
