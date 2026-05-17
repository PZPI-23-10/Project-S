using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Harvesting;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class TooltipAndInteractionPromptEditModeTests
    {
        private static readonly Vector3 RayOrigin = new Vector3(1000f, 1000f, 1000f);
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void TooltipFormatsWeaponDetails()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            _objects.Add(weapon);
            weapon.ItemName = "Test Axe";
            weapon.Kind = ItemKind.Weapon;
            weapon.Weight = 2f;
            weapon.Description = "Test weapon.";
            weapon.DamageProfile = new List<DamageInstance>
            {
                new DamageInstance { Type = DamageType.Slashing, Amount = 12f }
            };
            weapon.PoiseDamage = 8f;
            weapon.AttackSpeedMultiplier = 0.75f;
            weapon.MaxComboHits = 2;
            weapon.BlockMitigation = 0.5f;
            weapon.ParryWindow = 0.2f;
            weapon.HarvestTool = HarvestToolType.Axe;

            string header = TooltipUI.FormatHeader(weapon);
            string details = TooltipUI.FormatDetails(weapon);

            Assert.That(header, Does.Contain("Weapon"));
            Assert.That(header, Does.Contain("2"));
            Assert.That(details, Does.Contain("Slashing 12"));
            Assert.That(details, Does.Contain("Баланс: 8"));
            Assert.That(details, Does.Contain("Инструмент: Axe"));
        }

        [Test]
        public void TooltipFormatsConsumableDetails()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            _objects.Add(item);
            item.ItemName = "Potion";
            item.Kind = ItemKind.Consumable;
            item.IsStackable = true;
            item.MaxStack = 5;
            item.HealthRestoreAmount = 40f;
            item.HungerRestoreAmount = 10f;
            item.StaminaRestoreAmount = 3f;
            item.SpecialEffect = ConsumableSpecialEffectType.HomeTeleport;
            item.SpecialEffectDelaySeconds = 5f;

            string header = TooltipUI.FormatHeader(item);
            string details = TooltipUI.FormatDetails(item);

            Assert.That(header, Does.Contain("Стак: 5"));
            Assert.That(details, Does.Contain("Здоровье: +40"));
            Assert.That(details, Does.Contain("Голод: +10"));
            Assert.That(details, Does.Contain("Стамина: +3"));
            Assert.That(details, Does.Contain("телепорт домой"));
        }

        [Test]
        public void TooltipClampKeepsPositionInsideCanvas()
        {
            Vector2 clamped = TooltipUI.ClampAnchoredPosition(
                new Vector2(1000f, 1000f),
                new Vector2(200f, 100f),
                new Vector2(80f, 40f),
                new Vector2(0.5f, 0f));

            Assert.That(clamped.x, Is.EqualTo(60f).Within(0.001f));
            Assert.That(clamped.y, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void PlayerInteractorResolvesPickupHover()
        {
            var interactor = CreateInteractor(3f);
            var item = ScriptableObject.CreateInstance<ItemData>();
            _objects.Add(item);
            item.ItemName = "Wood";

            var pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(pickupObject);
            pickupObject.transform.position = RayOrigin + Vector3.forward * 2f;
            var pickup = pickupObject.AddComponent<ItemPickup>();
            pickup.Item = item;
            pickup.Amount = 3;
            Physics.SyncTransforms();

            Assert.That(interactor.TryGetHoverInfo(out var hoverInfo), Is.True);
            Assert.That(hoverInfo.Title, Is.EqualTo("Wood x3"));
            Assert.That(hoverInfo.ActionText, Is.EqualTo("E - Поднять"));
            Assert.That(hoverInfo.Pickup, Is.EqualTo(pickup));
        }

        [Test]
        public void PlayerInteractorResolvesInteractableHover()
        {
            var interactor = CreateInteractor(3f);
            var interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(interactableObject);
            interactableObject.transform.position = RayOrigin + Vector3.forward * 2f;
            var interactable = interactableObject.AddComponent<TestInteractable>();
            Physics.SyncTransforms();

            Assert.That(interactor.TryGetHoverInfo(out var hoverInfo), Is.True);
            Assert.That(hoverInfo.Title, Is.EqualTo("Test Station"));
            Assert.That(hoverInfo.ActionText, Is.EqualTo("E - Взаимодействовать"));
            Assert.That(hoverInfo.Interactable, Is.EqualTo(interactable));
        }

        [Test]
        public void PlayerInteractorReturnsFalseWhenTargetIsOutOfRange()
        {
            var interactor = CreateInteractor(1f);
            var interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(interactableObject);
            interactableObject.transform.position = RayOrigin + Vector3.forward * 3f;
            interactableObject.AddComponent<TestInteractable>();
            Physics.SyncTransforms();

            Assert.That(interactor.TryGetHoverInfo(out _), Is.False);
        }

        [Test]
        public void InventoryUISwitchesStationContextToHandWhenPlayerMovesTooFar()
        {
            var uiObject = new GameObject("Inventory UI");
            _objects.Add(uiObject);
            var ui = uiObject.AddComponent<InventoryUI>();

            var inventoryPanel = new GameObject("Inventory Panel");
            _objects.Add(inventoryPanel);
            var contextPanel = new GameObject("Context Panel");
            _objects.Add(contextPanel);

            SetPrivateField(ui, "_inventoryPanel", inventoryPanel);
            SetPrivateField(ui, "_contextPanel", contextPanel);

            var player = new GameObject("Player");
            _objects.Add(player);
            var station = new GameObject("Station");
            _objects.Add(station);
            player.transform.position = Vector3.zero;
            station.transform.position = Vector3.forward;

            ui.OpenWithCraftingContext(CraftingContext.Workbench, station.transform, player.transform, 2f);
            Assert.That(inventoryPanel.activeSelf, Is.True);
            Assert.That(contextPanel.activeSelf, Is.True);

            player.transform.position = Vector3.forward * 5f;
            InvokePrivate(ui, "Update");

            Assert.That(inventoryPanel.activeSelf, Is.True);
            Assert.That(contextPanel.activeSelf, Is.True);
            Assert.That(GetPrivateField<CraftingContext>(ui, "_currentCraftingContext"), Is.EqualTo(CraftingContext.Hand));
        }

        private PlayerInteractor CreateInteractor(float interactDistance)
        {
            var cameraObject = new GameObject("Interactor Camera");
            _objects.Add(cameraObject);
            cameraObject.transform.position = RayOrigin;
            cameraObject.transform.rotation = Quaternion.identity;
            cameraObject.AddComponent<Camera>();
            var interactor = cameraObject.AddComponent<PlayerInteractor>();
            SetPrivateField(interactor, "_interactDistance", interactDistance);
            InvokePrivate(interactor, "Awake");
            return interactor;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var type = target.GetType();
            MethodInfo method = null;
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(method, Is.Not.Null, $"Method {methodName} was not found.");
            method.Invoke(target, args);
        }

        private class TestInteractable : MonoBehaviour, IInteractable
        {
            public string InteractionPrompt => "Test Station";
            public bool WasInteracted { get; private set; }

            public void Interact(PlayerInteractor interactor)
            {
                WasInteracted = true;
            }
        }
    }
}
