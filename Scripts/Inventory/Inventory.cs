using Godot;
using System.Collections.Generic;

namespace CosmocrushGD;

public enum BulletType
{
    Medium
}

public partial class Inventory : Node2D
{
    [Export]
    private Label mediumBulletsLabel;

    public Dictionary<BulletType, int> Bullets = new()
    {
        { BulletType.Medium, 100 }
    };

    private Dictionary<BulletType, Label> bulletLabels;

    public override void _Ready()
    {
        bulletLabels = new()
        {
            { BulletType.Medium, mediumBulletsLabel } // Correctly assign the Label
        };

        UpdateBulletLabels();
    }

    public void UseAmmo(BulletType type, int amount)
    {
        if (Bullets.ContainsKey(type))
        {
            Bullets[type] -= amount;

            if (Bullets[type] < 0)
            {
                Bullets[type] = 0;
            }

            UpdateBulletLabel(type);
        }
    }

    public int GetAmmo(BulletType type)
    {
        return Bullets.ContainsKey(type) ? Bullets[type] : 0;
    }

    private void UpdateBulletLabels()
    {
        foreach (KeyValuePair<BulletType, Label> pair in bulletLabels)
        {
            UpdateBulletLabel(pair.Key);
        }
    }

    private void UpdateBulletLabel(BulletType type)
    {
        if (bulletLabels.ContainsKey(type))
        {
            bulletLabels[type].Text = Bullets[type].ToString();
        }
    }
}