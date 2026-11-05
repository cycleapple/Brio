using Brio.Game.Actor.Appearance;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Sheets;

[Sheet("HairMakeType", 3374538714u)]
public struct BrioHairMakeType : IExcelRow<BrioHairMakeType>
{
    public const int EntryCount = 100;

    public BrioHairMakeType()
    {
    }

    public uint RowId { get; private set; }
    public ExcelPage ExcelPage { get; private set; }
    public uint RowOffset { get; private set; }

    public RowRef<Race> Race { get; private set; }
    public RowRef<Tribe> Tribe { get; private set; }
    public Genders Gender { get; private set; }

    public RowRef<CharaMakeCustomize>[] HairStyles = new RowRef<CharaMakeCustomize>[EntryCount];
    public RowRef<CharaMakeCustomize>[] FacePaints = new RowRef<CharaMakeCustomize>[EntryCount];

    public static BrioHairMakeType Create(ExcelPage page, uint offset, uint row)
    {
        var native = new HairMakeType(page, offset, row);
        var hairMenu = native.CharaMakeStruct[0];
        var faceMenu = native.CharaMakeStruct[7];
        foreach (var menu in native.CharaMakeStruct) {
            if (menu.Customize == (uint)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.HairStyle) hairMenu = menu;
            if (menu.Customize == (uint)Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex.Facepaint) faceMenu = menu;
        }
        var brioHairMakeType = new BrioHairMakeType {
            RowId = row, ExcelPage = page, RowOffset = offset,
            Race = native.Race, Tribe = native.Tribe, Gender = (Genders)native.Gender
        };
        for (var i = 0; i < EntryCount; i++) {
            brioHairMakeType.HairStyles[i] = new RowRef<CharaMakeCustomize>(page.Module, hairMenu.SubMenuParam[i], page.Language);
            brioHairMakeType.FacePaints[i] = new RowRef<CharaMakeCustomize>(page.Module, faceMenu.SubMenuParam[i], page.Language);
        }
        return brioHairMakeType;
    }

    public static IEnumerable<CharaMakeCustomize> GetHairStyles(ActorCustomize customize)
    {
        var HairMakeType = GameDataProvider.Instance.DataManager.GetExcelSheet<BrioHairMakeType>().Where(x => x.Gender == customize.Gender && x.Race.RowId == (uint)customize.Race && x.Tribe.RowId == (uint)customize.Tribe);

        foreach(var item in HairMakeType)
        {
            foreach(var item2 in item.HairStyles)
            {
                if(item2.IsValid)
                    yield return item2.Value;
            }
        }
    }

    public static IEnumerable<CharaMakeCustomize> GetFacePaints(ActorCustomize customize)
    {
        var FacePaints = GameDataProvider.Instance.DataManager.GetExcelSheet<BrioHairMakeType>().Where(x => x.Gender == customize.Gender && x.Race.RowId == (uint)customize.Race && x.Tribe.RowId == (uint)customize.Tribe);

        foreach(var item in FacePaints)
        {
            foreach(var item2 in item.FacePaints)
            {
                if(item2.IsValid)
                    yield return item2.Value;
            }
        }
    }
}
