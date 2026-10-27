using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MorningLedger.Ledger;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace MorningLedger.UI;

internal sealed class LedgerMenu : IClickableMenu
{
    private const int RowHeight = 40;
    private const int IconSize = 32;
    private const int SidePadding = 44;
    private const int HeaderSpace = 104;

    private static readonly Rectangle UpArrowSource = new(421, 459, 11, 12);
    private static readonly Rectangle DownArrowSource = new(421, 472, 11, 12);

    private readonly IReadOnlyList<LedgerSection> sections;
    private readonly ISet<string> collapsed;
    private readonly string title;
    private readonly string dateText;
    private readonly List<Row> rows = new();
    private Rectangle content;
    private int scroll;

    private sealed record Row(string Text, Color Color, bool IsHeader = false, string? SectionKey = null, Texture2D? Icon = null, Rectangle IconSource = default);

    /// <param name="collapsed">Section keys the player has collapsed; kept by the caller so it survives reopening.</param>
    public LedgerMenu(IReadOnlyList<LedgerSection> sections, ISet<string> collapsed, string title, string dateText)
    {
        this.sections = sections;
        this.collapsed = collapsed;
        this.title = title;
        this.dateText = dateText;
        Layout();
    }

    private int VisibleRows => Math.Max(1, content.Height / RowHeight);

    /// <summary>Sizes the panel to fit its rows (up to the screen height), so a quiet morning gets a small panel.</summary>
    private void Layout()
    {
        int w = Math.Min(980, Game1.uiViewport.Width - 64);
        int maxHeight = Math.Min(760, Game1.uiViewport.Height - 96);
        BuildRows(w - SidePadding * 2);

        int h = Math.Clamp(HeaderSpace + 40 + Math.Max(rows.Count, 3) * RowHeight, 260, maxHeight);
        initialize((Game1.uiViewport.Width - w) / 2, (Game1.uiViewport.Height - h) / 2 + 16, w, h, showUpperRightCloseButton: true);
        content = new Rectangle(xPositionOnScreen + SidePadding, yPositionOnScreen + HeaderSpace, width - SidePadding * 2, height - HeaderSpace - 32);
        ClampScroll();
    }

    private void ClampScroll()
    {
        scroll = Math.Clamp(scroll, 0, Math.Max(0, rows.Count - VisibleRows));
    }

    private void BuildRows(int contentWidth)
    {
        rows.Clear();
        int textWidth = contentWidth - IconSize - 16;

        foreach (LedgerSection section in sections)
        {
            bool isCollapsed = collapsed.Contains(section.Key);
            rows.Add(new Row($"{(isCollapsed ? "+" : "-")} {section.Title} ({section.EntryCount})", Game1.textColor, IsHeader: true, SectionKey: section.Key));
            if (isCollapsed)
                continue;

            if (section.Lines.Count == 0)
            {
                AddWrapped(section.EmptyText, Game1.textColor * 0.6f, null, default, textWidth);
                continue;
            }

            foreach (LedgerLine line in section.Lines)
            {
                (Texture2D? icon, Rectangle source) = ResolveIcon(line);
                AddWrapped(line.Text, line.Color ?? Game1.textColor, icon, source, textWidth);
            }
        }
    }

    private void AddWrapped(string text, Color color, Texture2D? icon, Rectangle source, int textWidth)
    {
        string[] wrapped = Game1.parseText(text, Game1.smallFont, textWidth).Split('\n');
        for (int i = 0; i < wrapped.Length; i++)
            rows.Add(i == 0 ? new Row(wrapped[i], color, Icon: icon, IconSource: source) : new Row(wrapped[i], color));
    }

    private static (Texture2D?, Rectangle) ResolveIcon(LedgerLine line)
    {
        if (line.ItemId is not null)
        {
            var data = ItemRegistry.GetDataOrErrorItem(line.ItemId);
            return (data.GetTexture(), data.GetSourceRect());
        }
        if (line.NpcName is not null && Game1.getCharacterFromName(line.NpcName) is NPC npc)
            return (npc.Sprite.Texture, npc.getMugShotSourceRect());
        return (null, default);
    }

    private void Scroll(int delta)
    {
        int old = scroll;
        scroll = Math.Clamp(scroll + delta, 0, Math.Max(0, rows.Count - VisibleRows));
        if (scroll != old)
            Game1.playSound("shiny4");
    }

    public override void receiveScrollWheelAction(int direction)
    {
        Scroll(direction > 0 ? -3 : 3);
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
        if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key) || key == Keys.Up)
            Scroll(-1);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key) || key == Keys.Down)
            Scroll(1);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        base.receiveGamePadButton(b);
        if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp or Buttons.RightThumbstickUp)
            Scroll(-1);
        else if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown or Buttons.RightThumbstickDown)
            Scroll(1);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (!content.Contains(x, y))
            return;

        int index = scroll + (y - content.Y) / RowHeight;
        if (index < rows.Count && rows[index].IsHeader && rows[index].SectionKey is string key)
        {
            if (!collapsed.Remove(key))
                collapsed.Add(key);
            Game1.playSound("smallSelect");
            // Keep the panel where it is so the header stays under the cursor.
            BuildRows(content.Width);
            ClampScroll();
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        Layout();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.4f);
        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        SpriteText.drawStringWithScrollCenteredAt(b, title, xPositionOnScreen + width / 2, yPositionOnScreen - 40);
        Vector2 dateSize = Game1.smallFont.MeasureString(dateText);
        Utility.drawTextWithShadow(b, dateText, Game1.smallFont, new Vector2(xPositionOnScreen + (width - dateSize.X) / 2, yPositionOnScreen + 48), Game1.textColor);

        int lineHeight = (int)Game1.smallFont.MeasureString("Ay").Y;
        for (int i = 0; i < VisibleRows && scroll + i < rows.Count; i++)
        {
            Row row = rows[scroll + i];
            int rowY = content.Y + i * RowHeight;
            int textX = content.X;

            if (row.IsHeader)
            {
                b.Draw(Game1.staminaRect, new Rectangle(content.X, rowY + RowHeight - 4, content.Width, 2), Game1.textColor * 0.35f);
            }
            else
            {
                textX += IconSize + 12;
                if (row.Icon is not null && row.IconSource.Width > 0)
                {
                    // Fit item sprites (16x16) and portrait mugshots (taller) into the same square, keeping aspect.
                    float scale = (float)IconSize / Math.Max(row.IconSource.Width, row.IconSource.Height);
                    int w = (int)(row.IconSource.Width * scale), h = (int)(row.IconSource.Height * scale);
                    var dest = new Rectangle(content.X + (IconSize - w) / 2, rowY + (RowHeight - h) / 2, w, h);
                    b.Draw(row.Icon, dest, row.IconSource, Color.White);
                }
            }

            Utility.drawTextWithShadow(b, row.Text, Game1.smallFont, new Vector2(textX, rowY + (RowHeight - lineHeight) / 2), row.Color);
        }

        if (scroll > 0)
            b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + width - 64, content.Y - 8), UpArrowSource, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
        if (scroll + VisibleRows < rows.Count)
            b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + width - 64, content.Bottom - 28), DownArrowSource, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);

        base.draw(b);
        drawMouse(b);
    }
}
