using System.Collections;
using System.Text;
using GlobalEnums;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParryMod
{
    // Kayıt dosyasına (her kayıt için ayrı) yazılan mod bilgileri.
    public class ParryLocalSettings
    {
        public bool ShinobiGot;
        public bool ShinobiNew;
        public bool ShinobiEquipped;
        public bool GodseekerGiven;   // Godseeker modunda tılsım bir kez verildi mi
    }

    // Parry tuşu: klavye ve oyun kolu. Oyunun ayarlar menüsündeki gibi Mods > Parry sayfasından değiştirilir.
    public class ParryKeyBinds : InControl.PlayerActionSet
    {
        public InControl.PlayerAction Parry;
        public ParryKeyBinds()
        {
            Parry = CreatePlayerAction("Parry");
            Parry.AddDefaultBinding(InControl.Key.F);
            Parry.AddDefaultBinding(InControl.InputControlType.RightStickButton);
        }
    }

    // Bütün kayıtlar için ortak ayarlar (Hollow Knight'ın kayıt klasöründe ParryMod.GlobalSettings.json).
    // Tuşlar dosyaya düz yazı olarak kaydedilir (ör. "ParryKey": "F", "ParryButton": "RightStickButton").
    // (API'nin hazır kaydedicisi kol düğmesini saklamıyordu: oyundan çıkıp girince kol ayarı gidiyordu.)
    public class ParryGlobalSettings
    {
        public string ParryKey = "F";
        public string ParryButton = "RightStickButton";
        // Ses seviyeleri (0-10). 5 = modun asıl ses seviyesi, 10 = iki katı, 0 = sessiz.
        public int ParryVolume = 5;
        public int BigParryVolume = 5;
        [Newtonsoft.Json.JsonIgnore]
        public ParryKeyBinds Keybinds = new ParryKeyBinds();

        // Menüdeki atamaları dosyaya yazılacak hale getir.
        public void ReadFromBinds()
        {
            if (Keybinds == null) return;
            string key = null, btn = null;
            foreach (InControl.BindingSource b in Keybinds.Parry.Bindings)
            {
                if (b is InControl.KeyBindingSource kb && key == null && kb.Control.IncludeCount > 0) key = kb.Control.GetInclude(0).ToString();
                else if (b is InControl.DeviceBindingSource db && btn == null) btn = db.Control.ToString();
            }
            ParryKey = key ?? "";
            ParryButton = btn ?? "";
        }

        // Dosyadaki tuşları menüye/oyuna uygula.
        public void ApplyToBinds()
        {
            if (Keybinds == null) Keybinds = new ParryKeyBinds();
            InControl.PlayerAction a = Keybinds.Parry;
            a.ClearBindings();
            try { if (!string.IsNullOrEmpty(ParryKey)) a.AddBinding(new InControl.KeyBindingSource((InControl.Key)System.Enum.Parse(typeof(InControl.Key), ParryKey))); }
            catch (System.Exception) { a.AddBinding(new InControl.KeyBindingSource(InControl.Key.F)); }
            try { if (!string.IsNullOrEmpty(ParryButton)) a.AddBinding(new InControl.DeviceBindingSource((InControl.InputControlType)System.Enum.Parse(typeof(InControl.InputControlType), ParryButton))); }
            catch (System.Exception) { a.AddBinding(new InControl.DeviceBindingSource(InControl.InputControlType.RightStickButton)); }
        }
    }

    // Kopyalanan kaydırıcıdayken Esc / B ile geri dönmek için
    public class ParrySliderCancel : MonoBehaviour, UnityEngine.EventSystems.ICancelHandler
    {
        public System.Action OnCancelAction;
        public void OnCancel(UnityEngine.EventSystems.BaseEventData e) { OnCancelAction?.Invoke(); }
    }

    public class ParryMod : Mod, ITogglableMod, ILocalSettings<ParryLocalSettings>, IGlobalSettings<ParryGlobalSettings>, ICustomMenuMod
    {
        private static ParryGlobalSettings _gs = new ParryGlobalSettings();
        public void OnLoadGlobal(ParryGlobalSettings s)
        {
            _gs = s ?? new ParryGlobalSettings();
            _gs.ApplyToBinds();
        }
        public ParryGlobalSettings OnSaveGlobal() { _gs.ReadFromBinds(); return _gs; }
        internal static float ParryVolumeMul => Mathf.Clamp(_gs != null ? _gs.ParryVolume : 5, 0, 10) / 5f;
        internal static float BigParryVolumeMul => Mathf.Clamp(_gs != null ? _gs.BigParryVolume : 5, 0, 10) / 5f;

        // Mods menüsünde "Parry" sayfası: klavye tuşu ve oyun kolu düğmesi.
        public bool ToggleButtonInsideMenu => false;
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            System.Action<UnityEngine.UI.MenuSelectable> back = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu);
            return new Modding.Menu.MenuBuilder(UIManager.instance.UICanvas.gameObject, "ParryModMenu")
                .CreateTitle("Parry", MenuTitleStyle.vanillaStyle)
                .CreateContentPane(Modding.Menu.RectTransformData.FromSizeAndPos(
                    new Modding.Menu.RelVector2(new Vector2(1920f, 903f)),
                    new Modding.Menu.AnchoredPosition(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f))))
                .CreateControlPane(Modding.Menu.RectTransformData.FromSizeAndPos(
                    new Modding.Menu.RelVector2(new Vector2(1920f, 259f)),
                    new Modding.Menu.AnchoredPosition(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -502f))))
                .SetDefaultNavGraph(new Modding.Menu.ChainedNavGraph())
                .AddContent(Modding.Menu.RegularGridLayout.CreateVerticalLayout(105f), c =>
                {
                    c.AddKeybind("ParryKeyBind", _gs.Keybinds.Parry,
                        new Modding.Menu.Config.KeybindConfig { Label = "Parry (keyboard)", CancelAction = back });
                    c.AddButtonBind("ParryButtonBind", _gs.Keybinds.Parry,
                        new Modding.Menu.Config.ButtonBindConfig { Label = "Parry (controller)", CancelAction = back }, out _);
                    AddVolumeRow(c, "ParryVolume", "Parry Volume", () => _gs.ParryVolume, v => _gs.ParryVolume = v, back);
                    AddVolumeRow(c, "HeavyParryVolume", "Heavy Parry Volume", () => _gs.BigParryVolume, v => _gs.BigParryVolume = v, back);
                })
                .AddControls(new Modding.Menu.SingleContentLayout(new Modding.Menu.AnchoredPosition(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f))), c =>
                {
                    c.AddMenuButton("BackButton", new Modding.Menu.Config.MenuButtonConfig
                    {
                        Label = "Back", CancelAction = back, SubmitAction = back,
                        Style = Modding.Menu.Config.MenuButtonStyle.VanillaStyle, Proceed = true
                    });
                })
                .Build();
        }

        private static void AddVolumeRow(Modding.Menu.ContentArea c, string id, string label,
            System.Func<int> get, System.Action<int> set, System.Action<UnityEngine.UI.MenuSelectable> back)
        {
            GameObject src = null; Vector2 size = Vector2.zero;
            try
            {
                UnityEngine.UI.Slider[] all = UIManager.instance.audioMenuScreen.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
                if (all.Length > 0)
                {
                    // Kaydırıcıdan yukarı çık: içinde tek kaydırıcı olan en üst nesne = satır (yazı + çubuk + sayı)
                    Transform t = all[0].transform;
                    while (t.parent != null && t.parent.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Length == 1) t = t.parent;
                    src = t.gameObject;
                    size = ((RectTransform)t).rect.size;
                }
            }
            catch (System.Exception e) { Modding.Logger.Log("[ParryMod] Ses kaydırıcısı bulunamadı: " + e.Message); src = null; }

            if (src != null)
            {
                try
                {
                    if (size.x < 10f || size.y < 10f) size = new Vector2(1500f, 105f);
                    c.AddStaticPanel(id, new Modding.Menu.RelVector2(new Vector2(size.x, 105f)), out GameObject panel);
                    bool wasActive = src.activeSelf;
                    src.SetActive(false);
                    GameObject row = Object.Instantiate(src, panel.transform, false);
                    src.SetActive(wasActive);
                    row.name = id + "Row";
                    // Oyunun ses ayarına yazan / yazıyı Türkçe-İngilizce çeviren bileşenleri sök
                    foreach (MonoBehaviour m in row.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        string tn = m.GetType().Name;
                        if (tn == "MenuAudioSlider" || tn.Contains("Locali")) Object.DestroyImmediate(m);
                    }
                    RectTransform rt = (RectTransform)row.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = size;
                    rt.anchoredPosition = Vector2.zero;

                    UnityEngine.UI.Slider sl = row.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                    UnityEngine.UI.Text valueText = null;
                    foreach (UnityEngine.UI.Text tx in row.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    {
                        if (valueText == null && int.TryParse((tx.text ?? "").Trim(), out _)) valueText = tx;
                        else tx.text = label;
                    }
                    sl.onValueChanged = new UnityEngine.UI.Slider.SliderEvent();   // oyunun kalıcı dinleyicilerini sil
                    sl.minValue = 0f; sl.maxValue = 10f; sl.wholeNumbers = true;
                    sl.SetValueWithoutNotify(Mathf.Clamp(get(), 0, 10));
                    if (valueText != null) valueText.text = Mathf.Clamp(get(), 0, 10).ToString();
                    sl.onValueChanged.AddListener(v =>
                    {
                        int iv = Mathf.RoundToInt(v);
                        set(iv);
                        if (valueText != null) valueText.text = iv.ToString();
                    });
                    // Ses menüsünden kopyalanan geri dönüş ayarı Esc'de "Options"a gidiyordu (bizimkiyle birlikte iki menü üst üste açılıyordu).
                    // Kopyadaki bütün geri dönüş işleyicileri susturulur; geriye sadece bizimki kalır.
                    foreach (MonoBehaviour m in row.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (m is UnityEngine.UI.MenuSelectable ms) { ms.cancelAction = GlobalEnums.CancelAction.DoNothing; continue; }
                        if (m is UnityEngine.EventSystems.ICancelHandler && !(m is UnityEngine.UI.Selectable))
                        {
                            Object.DestroyImmediate(m);
                        }
                    }
                    ParrySliderCancel pc = sl.gameObject.AddComponent<ParrySliderCancel>();
                    pc.OnCancelAction = () => back(null);
                    row.SetActive(true);
                    c.NavGraph.AddNavigationNode(sl);
                    return;
                }
                catch (System.Exception e) { Modding.Logger.Log("[ParryMod] Kaydırıcı kopyalanamadı, oklu seçeneğe dönülüyor: " + e); }
            }

            string[] levels = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            c.AddHorizontalOption(id, new Modding.Menu.Config.HorizontalOptionConfig
            {
                Label = label,
                Options = levels,
                ApplySetting = (_, i) => set(i),
                RefreshSetting = (o, _) => o.optionList.SetOptionTo(Mathf.Clamp(get(), 0, 10)),
                CancelAction = back,
                Style = Modding.Menu.Config.HorizontalOptionStyle.VanillaStyle
            }, out UnityEngine.UI.MenuOptionHorizontal opt);
            opt.SetOptionTo(Mathf.Clamp(get(), 0, 10));
        }


        // =====================================================
        //  AYARLAR - sayıları değiştirip tekrar derleyerek oyna
        // =====================================================

        // Parry tuşu artık ayarlardan (Mods > Parry): klavye + oyun kolu. Varsayılan: F ve sağ çubuğa basma (R3).
        // Bu animasyonlar oynarken parry tuşu çalışmaz: yere iniş, sert iniş, çift zıplama, duvardan zıplama.
        private static readonly string[] ParryBlockedClips = { "Land", "HardLand", "Hard Land", "Double Jump", "Walljump", "Wall Jump", "WallJump" };
        private static string ParryBlockedAnim(HeroController h)
        {
            if (h == null) return null;
            if (h.cState.doubleJumping) return "Double Jump";
            if (h.cState.wallJumping) return "Wall Jump";
            try
            {
                tk2dSpriteAnimator a = h.GetComponent<tk2dSpriteAnimator>();
                if (a != null && a.CurrentClip != null && a.Playing && System.Array.IndexOf(ParryBlockedClips, a.CurrentClip.name) >= 0) return a.CurrentClip.name;
            }
            catch (System.Exception) { }
            return null;
        }

        private static bool ParryPressed() { return _gs != null && _gs.Keybinds != null && _gs.Keybinds.Parry.WasPressed; }

        // Tuşa bastıktan sonra parry'nin geçerli olduğu süre (saniye).
        // Küçüldükçe zorlaşır. 0.20 = zor, 0.35 = orta, 0.50 = rahat.
        private const float ParryWindowBase = 0.30f;
        private const float ParryWindowShinobi = 0.38f;     // Shinobi's Charm takılıyken
        private float ParryWindow => ShinobiOn ? ParryWindowShinobi : ParryWindowBase;
        // Bosslarda max savrulan saldırılar (büyük parry) için daha dar pencere: tuşa bastıktan sonra bu süre içinde gelmeli.
        // (%3 şans eseri büyük parry'ler normal pencereyi kullanır.)
        private const float HeavyParryWindowBase = 0.20f;
        private const float HeavyParryWindowShinobi = 0.30f; // Shinobi's Charm takılıyken
        private float HeavyParryWindow => ShinobiOn ? HeavyParryWindowShinobi : HeavyParryWindowBase;
        private float _windowStartTime = -10f;

        // Boşa basınca tekrar basabilmek için beklenmesi gereken süre (saniye).
        // Pencerenin belirgin şekilde uzun olmalı, yoksa F'ye sürekli basmak parry'yi bedavaya getirir.
        // Başarılı parry bu beklemeyi sıfırlar.
        private const float ParryCooldown = 0.80f;

        // Parry'lenen saldırı nesnesinin seni tekrar vuramayacağı süre (saniye).
        // Dokunulmazlık değildir: sadece o saldırı yok sayılır, diğer her şey vurur.
        private const float ParryGrace = 0.4f;

        // Parry'lenen vuruş sana değmeye devam ederse (ör. içinden geçen boss) koruma uzar,
        // ama toplamda en fazla bu kadar sürer. Her vuruş AYRI takip edilir:
        // Slash1'i parry'lemek Slash2'den korumaz.
        private const float ParryGraceMax = 1.0f;
        // Pure Vessel üçlü vuruşu: parry'den sonraki koruma daha kısa, böylece sıradaki vuruş sana gerçekten değer (ve parry'lenebilir).
        private static readonly string[] ShortGraceRules = { "hk prime|Slash1", "hk prime|Slash 2", "hk prime|Slash 3" };
        private const float ShortParryGrace = 0.15f;
        private const float ShortParryGraceMax = 0.3f;
        private readonly System.Collections.Generic.HashSet<GameObject> _shortGrace = new System.Collections.Generic.HashSet<GameObject>();

        // Boss saldırı iptali: bu adlardaki vuruş parry'lenince bossun saldırısı iptal edilir.
        // Listede olmayan vuruşlar (ör. Slash1, Slash2) iptal etmez, saldırı devam eder.
        private static readonly string[] BossCancelOnParry =
        {
            "dash stab"   // Pure Vessel dash (log'da doğrulandı)
            // Üçlü vuruşun 3.'sü artık iptal etmiyor (çok savrulma veriyor).
        };

        // Bossun "Control" durumu bunlardan biriyken parry yapılırsa da saldırı iptal edilir.
        // Slash 1 ve Slash 2 yok: üçlü vuruşta 3.'yü de parry'lemen gerekir.
        private static readonly string[] BossCancelStates =
        {
            "Dash",     // Pure Vessel dash (log'da doğrulandı)
            // Pure Vessel void tendril (haritadan)
            "Tendril Antic", "Tendril Burst", "Tendril Start", "Tendril Hit", "T2", "T3", "Tendril Hit End"
        };

        // Başarılı boss parry'si sersemleme sayacına kaç vuruş olarak eklensin.
        // Normal bir nail vuruşu = 1. Pure Vessel 9'luk kombo ya da 11 toplam vuruşta sersemliyor.
        // Küsuratlı olabilir: 0.75 = 4 parry'de 3 vuruş. 0 yaparsan bu özellik kapanır.
        private const float ParryStunHitsBase = 0.75f;
        private const float ParryStunHitsShinobi = 1.25f;   // Shinobi's Charm takılıyken
        private float ParryStunHits => ShinobiOn ? ParryStunHitsShinobi : ParryStunHitsBase;

        // Bossun bu durumlarındaki saldırıyı parry'leyince Knight çok uzağa savrulur.
        // Kurallar BOSSA ÖZEL: "boss adının parçası|durum adı". (Grimm'in de "Slash 3" durumu var,
        // Pure Vessel kuralları ona yanlışlıkla uygulanmasın diye.)
        private static readonly string[] FarKnockbackRules =
        {
            // Pure Vessel dash ve Grimm matkap dalışı artık "max" savrulmada (ClawKnockbackRules).
            // Pure Vessel ve Hollow Knight: üçlü vuruşun 3.'sü (iptal yok) ve Hollow Knight'ın yukarıdan saplaması.
            "hk prime|Slash1", "hk prime|Slash 2", "hk prime|Slash 3", "hollow knight boss|Slash 3",   // Pure Vessel üçlünün üçü de "çok"
            "hollow knight boss|Stomp Antic", "hollow knight boss|Stomp Down",
            "hk prime|Stomp Antic", "hk prime|Stomp Down",   // Pure Vessel yukarıdan saplama (dikenli ceza aynen kalır)
            // Traitor Lord: yerden atıldığı dash vuruşu ve yukarıdan atıldığı vuruş: çok savrulma.
            "traitor lord|Attack Antic", "traitor lord|Feint", "traitor lord|Speed Check", "traitor lord|Set Speed",
            "traitor lord|Attack 1", "traitor lord|Attack Swipe",
            "traitor lord|Jump", "traitor lord|DSlash Antic", "traitor lord|DSlash", "traitor lord|Land",
            // Sly 3'lü saldırının ilk iki parçası (yerde kombo + atılarak kombo): çok savurur
            "sly|Slash Antic", "sly|Slash S1", "sly|Slash S2", "sly|Slash S3", "sly|Slash1 End",
            "sly|BSlash Face", "sly|BSlash Antic", "sly|BSlash Dash", "sly|BSlash S1", "sly|BSlash S2",
            "sly|BSlash S3", "sly|BSlash End",
            "sly|BSlash Antic 2", "sly|BSlash Dash 2", "sly|BSlash S1 2", "sly|BSlash S2 2", "sly|BSlash S3 2",
            // Sly odaklı yer saldırısı (dash'siz)
            "sly|GSlash Charged", "sly|GSlash 1", "sly|GSlash 2", "sly|GSlash 3", "sly|GSlash 4"
        };

        // Çok uzun savrulma kuralları. Şu an boş: Grimm pençesi artık normal savrulma + saldırı iptali kullanıyor.
        // Süre x6, hız x1.6 = normal ağır savrulmanın yaklaşık 10 katı.
        // Çok ciddi savrulma: Sly, Oro ve Mato'nun odaklı dash'i ve sonundaki kılıç.
        private static readonly string[] ClawKnockbackRules =
        {
            // Grey Prince Zote: kılıcını sallayarak hücum (parry'lenince arkasına düşüp 1 sn sersemler)
            "grey prince|Charge Antic", "grey prince|Charge Start", "grey prince|Charge L", "grey prince|Charge R",
            "grey prince|Turn L", "grey prince|Turn R", "grey prince|To CT", "grey prince|Charge Through", "grey prince|Charge Through 2",
            "hk prime|Dash",                                   // Pure Vessel dash
            "hollow knight boss|Dash",                         // Hollow Knight dash (Pure Vessel'deki gibi)
            "grimm|AD Fire", "grimm|AD Edge",                  // Grimm matkap dalışı (iki versiyon; parry'lenince sersemler)
            "sly|Dashing L", "sly|Dashing R", "sly|D Slash S1", "sly|DSlash S2",
            "oro|Dashing L", "oro|Dashing R", "oro|D Slash", "oro|D Slash Recover",
            "mato|Dashing L", "mato|Dashing R", "mato|D Slash", "mato|D Slash Recover",
            // Sheo pembe (odaklı yer) saldırısı: max savrulma
            "sheo|GSlash Charged", "sheo|Gslash1", "sheo|GSlash 2", "sheo|GSlash 3", "sheo|GSlash 4"
        };
        private const float ClawKnockbackSpeed = 1.6f;

        private const float FarKnockbackSpeed = 1.3f;

        // Üçlü vuruşta (Slash1/2/3) biraz daha uzun savrulma: süre x1.6, hız x1.15 = yaklaşık 1,8 kat mesafe.
        // Bir sonraki vuruşu parry'leyebilmen için dash'tekinden az.
        private static readonly string[] SlashKnockbackRules =
        {
            // Hollow Knight üçlüsünün ilk ikisi orta (Pure Vessel'inkiler artık FarKnockbackRules'da: çok).
            "hollow knight boss|Slash1", "hollow knight boss|Slash 2",
            // Parry duruşundayken vurunca yaptığı karşı saldırı (counter): orta
            "hk prime|CS Antic", "hk prime|CSlash", "hollow knight boss|CS Antic", "hollow knight boss|CSlash",
            // Grey Prince Zote: kılıç kesiği (parry'lenince yere düşer)
            "grey prince|Stomp Slash", "grey prince|Slash End",
            // Grey Prince Zote: koşarak üstüne gelme
            "grey prince|Run Antic", "grey prince|Run", "grey prince|Run End",
            // False Knight / Failed Champion: kafasına vurulduktan sonra ortada sağa sola vurma (her vuruş): orta
            "false knight|R Attack Antic", "false knight|Rage Begin", "false knight|Rage", "false knight|Rage Slam",
            "false knight|Floor Crack?", "false knight|Anim End", "false knight|Turn",
            // Crystal Guardian ve Enraged Guardian: zıplayıp üstüne inme (parry'lenebilir): orta
            "beam miner|Aim Jump", "beam miner|Jump Antic", "beam miner|Launch", "beam miner|Rising", "beam miner|Falling", "beam miner|Land"
        };
        private static readonly string[] CgJumpStates = { "Aim Jump", "Jump Antic", "Launch", "Rising", "Falling", "Land" };

        // Crystal Guardian'ın her şeyi parry'lenemez, tek istisna zıplayıp üstüne indiği gövde vuruşu.
        private static bool ParryException(GameObject go)
        {
            try
            {
                if (go == null) return false;
                // Grimmkin'lerin attığı alev topları: sahnede Grimm yoksa parry'lenir (Grimm'in alev topları değişmez).
                if (NameChainStatic(go.transform).Contains("flameball") && !GrimmBossPresent()) return true;
                HealthManager hm = go.GetComponentInParent<HealthManager>();
                if (hm == null || hm.gameObject != go) return false;   // sadece gövdenin kendisi (kol vuruşu, ışınlar değil)
                if (!hm.gameObject.name.ToLowerInvariant().Contains("beam miner")) return false;
                PlayMakerFSM m = MainFsm(hm.gameObject);
                return m != null && System.Array.IndexOf(CgJumpStates, m.ActiveStateName) >= 0;
            }
            catch (System.Exception) { return false; }
        }
        private static string NameChainStatic(Transform t)
        {
            var sb = new StringBuilder();
            for (; t != null; t = t.parent) sb.Append(t.name.ToLowerInvariant()).Append('|');
            return sb.ToString();
        }
        private static bool GrimmBossPresent()
        {
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
                if (hm != null && hm.hp > 0 && hm.gameObject.name.Contains("Grimm Boss")) return true;
            return false;
        }
        private static bool UnparryableHit(GameObject go, string attackerInfo) { return (IsUnparryable(attackerInfo) || NormalUnparryable(go) || GpzUnparryable(go)) && !ParryException(go); }

        // ---------- Grey Prince Zote ----------
        // Sadece kılıcını salladığı saldırı (zıplayıp inince yaptığı kesik) parry'lenir; diğer her şeyi (gövde, hücum,
        // yerden dalgalar, balonlar, patlamalar) parry'lenmez. Zotelingler ayrı (parry'lenebilir).
        private static readonly string[] GpzSlashStates = { "Stomp Slash", "Slash End" };
        // Koşarak ve kılıcını sallayarak üstüne gelme (hücum): parry'lenebilir, orta savrulma (yere düşmez)
        private static readonly string[] GpzRunStates = { "Run Antic", "Run", "Run End",
            "Charge Antic", "Charge Start", "Charge L", "Charge R", "Turn L", "Turn R", "To CT", "Charge Through", "Charge Through 2" };
        private static float _gpzSeenUntil;   // GPZ savaşı sürüyor (bağlı olmayan vuruşlar için)
        private const float GpzChargeStunTime = 1.0f;      // hücum parry'sinden sonra yerde sersem kalma süresi
        private const float GpzFallBackSpeed = 9f;          // arkasına doğru düşerken yatay hız
        private IEnumerator GpzChargeStun(GameObject gpz, HeroController hero, string from)
        {
            PlayMakerFSM c = FsmByName(gpz, "Control");
            Rigidbody2D rb = gpz.GetComponent<Rigidbody2D>();
            if (c == null) yield break;
            float away = hero != null && hero.transform.position.x > gpz.transform.position.x ? -1f : 1f;   // Knight'ın tersi = arkası
            c.SendEvent("STUN");
            yield return null;
            float t0 = Time.time;
            while (gpz != null && c.ActiveStateName != "Stun" && Time.time - t0 < 2f)
            {
                if (rb != null && (c.ActiveStateName == "Stun Start" || c.ActiveStateName == "Stun Air"))
                    rb.velocity = new Vector2(away * GpzFallBackSpeed, rb.velocity.y);
                yield return null;
            }
            if (gpz == null || c.ActiveStateName != "Stun") {  yield break; }
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            // Yerde tam 1 sn: oyunun kendi bekleme süresi ne olursa olsun
            PauseFsm(c, true);
            yield return new WaitForSeconds(GpzChargeStunTime);
            if (gpz == null) yield break;
            PauseFsm(c, false);
            c.SetState("Jump Antic");
        }

        private static bool GpzUnparryable(GameObject go)
        {
            if (go == null) return false;
            HealthManager hm = go.GetComponentInParent<HealthManager>();
            if (hm == null) return Time.time < _gpzSeenUntil;              // dalgalar, patlamalar
            string n = hm.gameObject.name.ToLowerInvariant();
            if (n.StartsWith("zote balloon")) return true;                  // balonlar
            if (!n.StartsWith("grey prince")) return false;
            PlayMakerFSM c = FsmByName(hm.gameObject, "Control");
            string st = c != null ? c.ActiveStateName : "";
            bool slash = System.Array.IndexOf(GpzSlashStates, st) >= 0 || go.name.ToLowerInvariant().Contains("slash");
            bool run = System.Array.IndexOf(GpzRunStates, st) >= 0;
            return !slash && !run;
        }

        // Hafif savrulma (1.6 birim): Watcher Knight dikenli top halinde dönerek gelme saldırısı.
        private static readonly string[] LightKnockbackRules =
        {
            "black knight|Charge Antic", "black knight|Charge", "black knight|Hit Wall", "black knight|Scale Sparks"
        };

        // Grimm: bu durumlarda parry'lenince saldırı iptal olur ve Grimm ışınlanarak kaybolur (TELE OUT).
        // Grimm: bu durumlarda parry'lenince saldırı iptal olur, Grimm geri sıçrar (kendi "Evade"
        // hareketi) ve sonra ışınlanarak kaybolur (TELE OUT). Matkap dalışı İPTAL OLMAZ, devam eder.
        private static readonly string[] TeleOutCancelRules =
        {
            // matkaptan sonra yerdeki dash: hazırlığı, kendisi ve bitişi
            "grimm|GD Antic", "grimm|G Dash", "grimm|G Dash Recover"
        };

        // Zorlaştırma: Grimm'in yer dash'i parry'lenince geri kayıp ışınlandıktan sonra hemen
        // Knight'ın arkasında belirip pençe saldırısı yapar. false yaparsan sadece ışınlanır.
        private const bool GrimmDashParryAmbush = true;
        // Arkanda ne kadar uzakta belireceği (oyun birimi).
        private const float AmbushDistance = 5f;
        // Pusunun hızı: ışınlanma/belirme animasyon hızı çarpanı ve pençe hazırlık süresi çarpanı.
        private const float AmbushAnimSpeed = 2f;
        private const float AmbushWindupMul = 0.5f;

        // Grimm parry iptalinden sonraki geri kaymanın hızı (oyunun "Evade Speed" değeri çarpılır).
        private const float GrimmEvadeSpeedMul = 1.8f;

        // Parry'nin bedeli: pençe parry'sinden sonra gelen alev yarasası saldırısı daha hızlı.
        // Bekleme süreleri (hazırlık, yarasalar arası) bu oranla çarpılır: 0.5 = yarısı.
        private const float PunishFirebatWaitMul = 0.5f;
        // Yarasaların uçuş hızı bu oranla çarpılır.
        private const float PunishFirebatSpeedMul = 1.5f;

        // Grimm yandan pençe: parry'lenince geri kayar ve ışınlanmadan hemen alev yarasası atar.
        private static readonly string[] EvadeFirebatRules =
        {
            "grimm|Slash 1", "grimm|Slash 2", "grimm|Slash 3"
        };

        // Bu saldırılar parry'lenince yine de azaltılmış hasar alırsın (2 -> 1).
        private static readonly string[] ReducedDamageOnParryNames =
        {
            "nightmare firebat"   // Nightmare King Grimm alev yarasaları
        };

        private static bool MatchRule(string[] rules, string bossName, string state)
        {
            if (bossName == null || state == null) return false;
            string b = Canon(bossName);
            foreach (string r in rules)
            {
                int bar = r.IndexOf('|');
                if (bar < 0) continue;
                if (b.Contains(r.Substring(0, bar)) && r.Substring(bar + 1) == state) return true;
            }
            return false;
        }
        private const float SlashKnockbackSpeed = 1.15f;

        // Boss bu durumlardan birindeyken HİÇBİR saldırı parry'lenemez (Pure Vessel haritasından).
        private static readonly string[] UnparryableBossStates =
        {
            // Odaklanma ve etrafında patlama saldırısı
            "Focus Charge", "Ball Up", "Focus Burst", "Hit End", "Focus Recover", "Focus Wait"
        };

        // Void tendril saldırısının durumları: parry'lenebilir, parry'de iptal olur,
        // ama yine de azaltılmış hasar alırsın.
        private static readonly string[] TendrilStates =
        {
            "Tendril Antic", "Tendril Burst", "Tendril Start", "Tendril Hit", "T2", "T3", "Tendril Hit End"
        };

        // Tendril parry'sinde hasar bu kadar azalır (2 maske -> 1 maske). En az 1 hasar kalır.
        private const int TendrilParryDamageReduction = 1;

        // Kombo yön kilidi: aynı bossu bu süre içinde tekrar parry'lersen savrulma yönü değişmez (saniye).
        private const float PushLockTime = 1.0f;
        private GameObject _pushLockBoss;
        private bool _pushLockRight;
        private float _pushLockTime = -10f;

        // Boss saldırı iptali açık mı?
        private const bool BossCancelEnabled = true;

        // İptalden sonra bossun karşı parry/kaçış yapamayacağı, sadece bekleyeceği süre (saniye).
        // Bossun kendi bekleme süresi zaten 0,5 sn (haritadaki Idle Time).
        private const float BossCancelPause = 0.6f;

        // İptalden sonra bossun geçeceği durum için denenecek adlar (ilk bulunan kullanılır).
        private static readonly string[] BossIdleStateCandidates =
        {
            // Pure Vessel haritası: her saldırı "Idle Stance" ile biter -> Set Idle Time (0,5 sn) -> Idle -> yeni saldırı.
            "Idle Stance"
        };

        // Başarılı parry'de saldırıyı yapan düşmanı geri itip sersemletmeyi dener.
        // false yaparsan bu özellik kapanır.
        private const bool InterruptEnemies = true;

        // Bu candan yüksek düşmanlar (bosslar) sersemletilmez.
        private const int MaxInterruptHp = 120;

        // Parry'lenen düşmanın geri itilme gücü. Büyüdükçe daha uzağa gider.
        // 2.0 = çok uzak, 1.0 = orta, 0.6 = hafif (şimdiki), 0.3 = çok az.
        private const float EnemyPushStrength = 0.6f;

        // SADECE bu listedeki düşmanların saldırısı parry'de kesilir (küçük harfle yaz).
        // Saldırıyı yapan nesnenin ve üst nesnelerinin adında bu kelimelerden biri geçmesi yeterli.
        // Aşağıdakiler TAHMİN. ModLog'daki "Sahnede düşman" satırlarına bakıp gerçek adlarla tamamlayacağız.
        private static readonly string[] InterruptNames =
        {
            "zote",            // Zote the Mighty (tahmin)
            "grey prince",     // Grey Prince Zote (tahmin)
            "crawler",         // Crawlid (tahmin)
            "climber",         // Tiktik (tahmin)
            "zombie runner",   // Wandering Husk olabilir (log'da görüldü, hangisi emin değilim)
            "zombie hornhead", // Husk Hornhead (log'da doğrulandı)
            "zombie barger",   // Husk Bully
            "mossman_runner",  // Mosskin
            "maggot",          // Maggot (tahmin)
            "blocker",         // Baldur (tahmin)
            "flukeman",        // Flukemon, üst ve alt gövde (tahmin)
            "mosquito"         // Squit (tahmin)
        };

        // Görünce koşarak gelen düşmanlar. Parry'de bunların davranışı BAŞA SARILIR:
        // yarım kalan koşu/saldırı iptal olur, düşman seni yeniden görüp YENİ bir saldırı başlatır.
        // (Bu listedekiler yukarıdaki listede de olmalı.)
        private static readonly string[] ResetOnParryNames =
        {
            "zombie runner",   // Wandering Husk (log'da doğrulandı)
            "zombie hornhead", // Husk Hornhead (log'da doğrulandı)
            "zombie barger",   // Husk Bully
            "mossman_runner"   // Mosskin
        };

        // Bu saldırganlardan gelen parry'de Knight çok daha uzağa savrulur.
        // Pure Vessel ve Hornet. Şimdilik bu bosların TÜM saldırıları için geçerli,
        // log'dan dash saldırısının adını bulunca sadece ona daraltacağız.
        private static readonly string[] HeavyKnockbackNames =
        {
            "hk prime",    // Pure Vessel
            "hornet boss", // Hornet
            "grimm",       // Grimm, iki versiyon (uzak savrulmanın çalışması için uzun savrulma gerekli)
            "sly boss", "sheo boss", "oro <- brothers", "mato <- brothers",
            "galien hammer",
            "mage lord", "mage knight"   // Soul Master / Soul Tyrant, Soul Warrior (uzak savrulma için uzun savrulma gerekli)
        };




        // Parry'lenemeyen saldırıların isim parçaları (küçük harfle yaz).
        // Saldırıyı yapan nesnenin adında bunlardan biri geçiyorsa parry çalışmaz.
        // Bu liste ilk tahmindir; oyunda test edip ModLog'daki gerçek isimlere göre düzelteceğiz.
        private static readonly string[] Unparryable =
        {
            // (eski "infect" kuralı kaldırıldı: enfeksiyon saldırıları ve enfekte düşmanlar artık parry'lenebilir)
            "giant buzzer",     // Vengefly King: hiçbir saldırısı parry'lenmez
            // Grimm (iki versiyon), EnemyScout kaydından:
            "up ball",          // yukarı pençe sonundaki alev topu patlaması
            "flameball",        // balon saldırısının alev topları
            "grimm spike",      // yerden çıkan dikenler (Grimm Spike / Grimm Spike Holder)
            "nightmare spike",  // yerden çıkan dikenler (Nightmare King)
            "flare_pillar",     // alev sütunları (Nightmare King)
            "flame trail",      // dash'ten sonra yerde kalan alev izi (Nightmare King)
            "radiant",   // Radiance saldırıları
            "radiance",
            "absolute",
            // Galien: küçük çekiçler ve Galien'in kendi gövdesi (büyük çekiç parry'lenebilir)
            "galien mini hammer",
            "ghost warrior galien",
            // No Eyes: hiçbir şeyi parry'lenemez (kafalar ve gövde), dodge ile oynanır
            "no eyes",
            // Marmu: gövdesiyle vurur, parry'lenemez
            "ghost warrior marmu",
            // Pure Vessel'in yerden çıkan dikenleri hiçbir zaman parry'lenemez
            "hk plume",
            // Dung Defender / White Defender: sadece fırlattığı dışkı topları parry'lenir (gövde, kol, yerden çıkan sütunlar değil)
            "dung defender", "white defender", "dung pillar",
            // Soul Master / Soul Tyrant: yere çakılma ve yerden yayılan dalgalar, 2. faz gövdesi parry'lenemez
            "shockwave spurt", "quake hit", "lord phase2",
            // False Knight / Failed Champion: yere vurunca yerden giden dalgalar (gürz kendisi parry'lenir)
            "shockwave",
            // Hornet: yukarılara bıraktığı tuzak dikenler ve etrafında döndürdüğü iplik topu
            "hornet barb", "sphere ball",
            // Hive Knight: arılar (ağzından çıkan sürü ve yukarıdan düşen arılar)
            "mouth swarm", "bee dropper", "bee_parent",
            // Uumuu: elektrik saldırıları
            "mega jelly zap", "mega jelly multizap",
            // Oyundaki bütün patlamalar
            "explosion",
            // Crystal Guardian / Enraged Guardian: bütün saldırıları (gövde, kol, ışınlar, yukarıdan gelen sütunlar)
            "beam miner", "laser turret", "beam",
            // (Broken Vessel / Lost Kin enfeksiyon baloncukları artık parry'lenebilir: geriye savrulur)
        };

        // Soul Master / Soul Tyrant: sana doğru uçtuğu saldırı (parry'lenince çok uzağa savrulursun, stun barı %50 dolar).
        private static readonly string[] SoulChargeRules = { "mage lord|Charge Dir", "mage lord|Charge Left", "mage lord|Charge Right" };
        // Soul Warrior: yukarıdan çapraz dash (baktığın yönün tersine çok uzağa savrulursun).
        private static readonly string[] MageKnightStompRules = { "mage knight|Stomp Antic", "mage knight|Stomp Air", "mage knight|Stomp Slash" };
        // Soul Warrior: yerden dash (çok uzağa savrulursun, saldırısı orada kesilir).
        private static readonly string[] MageKnightDashRules = { "mage knight|Slash Antic", "mage knight|Dash", "mage knight|Slash" };

        // =====================================================

        private float _windowEnd;
        private float _nextParryAllowed;
        private bool _hitStopRunning;
        // Parry'lenen vuruşlar: x = parry anı, y = korumanın bittiği an
        // Grimm alev izi: dash parry'si sırasında ve sonrasında izin parry'yi "çalmaması" için.
        private float _trailGraceUntil;
        private static readonly string[] GrimmDashStates =
        {
            "AD Fire", "AD Edge", "GD Antic", "G Dash", "G Dash Recover"
        };
        private const float TrailGraceAfterParry = 1.0f;

        // Pure Vessel void tendril: parry'den sonra saldırıdan kalan patlama parçaları vurmasın.
        private float _tendrilGraceUntil;
        private const float TendrilGraceAfterParry = 1.5f;
        private float _lastGraceLog;

        private readonly System.Collections.Generic.Dictionary<GameObject, Vector2> _ignored =
            new System.Collections.Generic.Dictionary<GameObject, Vector2>();
        private static Sprite _ringSprite;

        public ParryMod() : base("Parry") { }

        public override string GetVersion() => "1.0.0.0";

        // ---------- Önceden yüklenen nesneler (başka sahnelerden) ----------
        // Pure Vessel'in 2. diken dalgası için üçüncü sondaki void tendril'ler (Dream_Final_Boss sahnesi).
        private const bool UseVoidTendrils = true;
        private static readonly string[] TendrilPaths =
        {
            "Boss Control/Abyss Pit/Tendrils 1/Tendril1",
            "Boss Control/Abyss Pit/Tendrils 2/Tendril2",
            "Boss Control/Abyss Pit/Tendrils 2/Tendril3",
            "Boss Control/Abyss Pit/Tendrils 1/Tendril4",
            "Boss Control/Abyss Pit/Tendrils 1/Tendril5"
        };
        private static readonly System.Collections.Generic.List<GameObject> _tendrilPrefabs = new System.Collections.Generic.List<GameObject>();
        // Zemini dolduran void (Radiance dövüşünün sonundaki kara çukur).
        private const bool UseVoidFloor = true;
        private const string AbyssPitPath = "Boss Control/Abyss Pit";
        private static GameObject _abyssPitPrefab;

        public override System.Collections.Generic.List<(string, string)> GetPreloadNames()
        {
            var list = new System.Collections.Generic.List<(string, string)>();
            if (UseVoidTendrils) foreach (string tp in TendrilPaths) list.Add(("Dream_Final_Boss", tp));
            if (UseVoidFloor) list.Add(("Dream_Final_Boss", AbyssPitPath));
            list.Add((ShinyScene, ShinyPath));   // King's Pass'teki tılsım parıltısı (Fury of the Fallen'ınki)
            return list;
        }

        public override void Initialize(System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, GameObject>> preloadedObjects)
        {
            if (preloadedObjects != null && preloadedObjects.TryGetValue("Dream_Final_Boss", out var objs))
            {
                _tendrilPrefabs.Clear();
                foreach (string tp in TendrilPaths)
                    if (objs.TryGetValue(tp, out GameObject g) && g != null) _tendrilPrefabs.Add(g);
                if (objs.TryGetValue(AbyssPitPath, out GameObject pit)) _abyssPitPrefab = pit;
            }
            if (preloadedObjects != null && preloadedObjects.TryGetValue(ShinyScene, out var so) && so.TryGetValue(ShinyPath, out GameObject sh))
                _shinyPrefab = sh;
            Initialize();
            RenderTendrilShapes();
            LoadVoidAtlas();
            LoadRoarFrames();
            ParryVisuals.Instance(this);   // parry çizim ve seslerini açılışta yükle (ilk parry'de takılma olmasın)
        }

        // ---------- Pure Vessel void görünümü (kendi çizilmiş atlas) ----------
        private const string VoidAtlasFile = "PV_Void_Atlas.png";   // mod klasörüne konacak resim
        private static Texture2D _voidAtlas;

        private void LoadVoidAtlas()
        {
            try
            {
                string path = ParryAssets.File("PureVessel", VoidAtlasFile);
                if (!System.IO.File.Exists(path)) {  return; }
                float t0 = Time.realtimeSinceStartup;
                byte[] data = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(data, false)) {  return; }
                tex.name = "PV Void Atlas";
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                if (tex.width % 4 == 0 && tex.height % 4 == 0) tex.Compress(false);   // bellekte 4 kat küçülsün
                tex.Apply(false, true);                                                // işlemci kopyasını bırak
                _voidAtlas = tex;
            }
            catch (System.Exception) {  }
        }


        // ---------- Pure Vessel void formu ----------
        private const int VoidFormParries = 10;        // en az bu kadar başarılı parry...
        private const int VoidFormStuns = 2;           // ...ve bu kadar stun olunca, sıradaki parry'de void formu
        private const string VoidPosePrefix = "HK_Prime_acknowledge";   // oyunda kullanılmayan "selam" animasyonunun kareleri (0000-0004)
        private const float VoidPoseFps = 8f;          // poz animasyonunun hızı
        private const float VoidPoseHold = 0.35f;       // poz animasyonu bitince son karede bu kadar durur, sonra bağırma
        private bool _voidPending;
        private const float VoidMinRoar = 1.2f;        // bağırma (ve renk geçişi) en az bu kadar sürer
        private const float VoidSpeedMul = 1.2f;       // saldırı hızı %20 artar
        private const int VoidExtraHp = 250;           // mevcut cana eklenir
        private const float VoidStunMul = 1.5f;        // stun barı %50 büyür
        private GameObject _pvWatched;
        private int _pvParries, _pvStuns;
        private bool _voidTransitionRunning, _voidFormDone;

        private void CountPvParry(GameObject pv)
        {
            WatchPv(pv);
            _pvParries++;
            if (!_voidFormDone && !_voidPending && !_voidTransitionRunning && _pvParries >= VoidFormParries && _pvStuns >= VoidFormStuns)
            {
                _voidPending = true;
            }
        }

        // Pure Vessel'i izlemeye başla (her karede VoidTick bakar).
        private void WatchPv(GameObject pv)
        {
            if (pv == null || _pvWatched == pv) return;
            _pvWatched = pv; _pvParries = 0; _pvStuns = 0; _voidFormDone = false; _voidTransitionRunning = false; _voidPending = false;
            _pvCtrl = MainFsm(pv); _pvPrevState = _pvCtrl != null ? _pvCtrl.ActiveStateName : "";
        }

        private PlayMakerFSM _pvCtrl;
        private string _pvPrevState = "";
        private static readonly string[] PvStunStates = { "Stun Start", "Stun Air", "Stun Fall", "Stun", "Quick Stun", "Stun Tele?" };
        private static readonly string[] PvStompStates = { "Stomp Antic", "Stomp Down", "Stomp Land" };

        // Her karede (Knight'ın Update'inden): stun sayar, void formunda saplama saldırısını yakalar, geçişi başlatır.
        private void VoidTick()
        {
            GameObject pv = _pvWatched;
            if (pv == null) return;
            if (_pvCtrl == null) { _pvCtrl = MainFsm(pv); if (_pvCtrl == null) return; }
            if (!_pvCtrl.enabled) return;   // geçiş sırasında kapalı
            string cur = _pvCtrl.ActiveStateName ?? "";
            string prev = _pvPrevState;
            if (cur == prev) return;
            _pvPrevState = cur;
            bool curStun = System.Array.IndexOf(PvStunStates, cur) >= 0, prevStun = System.Array.IndexOf(PvStunStates, prev) >= 0;
            if (curStun && !prevStun) { _pvStuns++;  }
            bool curStomp = System.Array.IndexOf(PvStompStates, cur) >= 0, prevStomp = System.Array.IndexOf(PvStompStates, prev) >= 0;
            if (_voidFormDone && curStomp && !prevStomp && !_pvPunishRunning && !_voidTransitionRunning)
            {
                GameManager.instance.StartCoroutine(PvPlumePunish(pv, true));
            }
            if (_voidPending && !_voidFormDone && !_voidTransitionRunning)
            {
                _voidPending = false;
                GameManager.instance.StartCoroutine(VoidTransition(pv, "parry " + _pvParries + ", stun " + _pvStuns));
            }
        }

        // Knight'ı oyunun "bağırma kilidi"ne sokar (savaş başındaki gibi: boss bağırırken Knight durur ve izler).
        private static bool RoarLock(bool enter, GameObject roarer = null)
        {
            HeroController h = HeroController.instance;
            if (h == null) return false;
            foreach (PlayMakerFSM f in h.GetComponents<PlayMakerFSM>())
                if (f.FsmName == "Roar Lock")
                {
                    // Savaş başındaki gibi: Knight bağıran boss'a göre durur.
                    if (enter && roarer != null) { var ro = f.FsmVariables.FindFsmGameObject("Roar Object"); if (ro != null) ro.Value = roarer; }
                    f.SendEvent(enter ? "ROAR ENTER" : "ROAR EXIT");
                    return true;
                }
            return false;
        }

        private const float VoidEvadeSpeed = 30f;     // geçişin başındaki geri sıçramanın hızı
        private const float VoidEvadeTime = 0.3f;      // geri sıçramanın süresi
        private const float VoidIdleBeforeRoar = 0.6f; // pozdan sonra bağırmadan önce normal duruşta bekleme
        private const float VoidRoarExtra = 1.0f;      // bağırma bu kadar saniye daha uzun sürer
        private const float VoidIdleAfter = 0.1f;      // toparlanmadan sonra saldırmadan önce normal duruşta bekleme

        private const float VoidRoarFade = 0.6f;       // bağırma sesinin solma süresi
        private const float VoidRoarFadeEarly = 0.2f;  // ses, animasyon bitmeden bu kadar önce tamamen susar

        // Bir adımdaki ses eylemlerinin oluşturduğu ses çalarlar (oyunun "storePlayer" değişkeni).
        private static System.Collections.Generic.List<AudioSource> StoredAudioPlayers(PlayMakerFSM m, string state)
        {
            var list = new System.Collections.Generic.List<AudioSource>();
            foreach (var st in m.FsmStates)
            {
                if (st.Name != state || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || a.GetType().Name != "AudioPlayerOneShotSingle") continue;
                    var sp = a.GetType().GetField("storePlayer")?.GetValue(a) as HutongGames.PlayMaker.FsmGameObject;
                    GameObject g = sp != null ? sp.Value : null;
                    AudioSource src = g != null ? g.GetComponent<AudioSource>() : null;
                    if (src != null && !list.Contains(src)) list.Add(src);
                }
            }
            return list;
        }

        private IEnumerator FadeRoarAudio(System.Collections.Generic.List<AudioSource> srcs, float wait, float fade)
        {
            if (srcs == null || srcs.Count == 0) yield break;
            var vol = new float[srcs.Count];
            for (int i = 0; i < srcs.Count; i++) vol[i] = srcs[i] != null ? srcs[i].volume : 1f;
            if (wait > 0f) yield return new WaitForSeconds(wait);
            float t0 = Time.time;
            while (Time.time - t0 < fade)
            {
                float k = 1f - (Time.time - t0) / fade;
                for (int i = 0; i < srcs.Count; i++) if (srcs[i] != null) srcs[i].volume = vol[i] * k;
                yield return null;
            }
            // Ses çalar havuzdan tekrar kullanılacak: durdur ve sesini eski haline getir.
            for (int i = 0; i < srcs.Count; i++) if (srcs[i] != null) { srcs[i].Stop(); srcs[i].volume = vol[i]; }
        }

        // Normal duruş animasyonu: bekleme boyunca oynamaya devam eder (donuk resim gibi durmasın).
        private IEnumerator IdleFor(GameObject pv, tk2dSpriteAnimator anim, Rigidbody2D rb, float time)
        {
            tk2dSpriteAnimationClip idle = anim != null && anim.Library != null ? anim.Library.GetClipByName("Idle") : null;
            if (idle != null) anim.Play(idle);
            float tEnd = Time.time + time;
            while (pv != null && Time.time < tEnd)
            {
                if (idle != null && !anim.IsPlaying(idle)) anim.Play(idle);
                HoldStill(rb);
                yield return null;
            }
        }

        private void HoldStill(Rigidbody2D rb) { if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y); }

        private float PlayStateClipByName(tk2dSpriteAnimator anim, string clip, float fallback)
        {
            if (anim == null || anim.Library == null) return fallback;
            tk2dSpriteAnimationClip c = anim.Library.GetClipByName(clip);
            if (c == null) {  return fallback; }
            try { anim.Play(c); } catch (System.Exception) { return fallback; }
            return c.fps > 0f && c.frames != null ? c.frames.Length / c.fps : fallback;
        }

        private float PlayStateClip(tk2dSpriteAnimator anim, PlayMakerFSM m, string state, float fallback)
        {
            string clip = GetStateClip(m, state);
            if (clip == null || anim == null) return fallback;
            try
            {
                anim.Play(clip);
                tk2dSpriteAnimationClip c = anim.Library.GetClipByName(clip);
                if (c != null && c.fps > 0f && c.frames != null) return c.frames.Length / c.fps;
            }
            catch (System.Exception) { }
            return fallback;
        }

        // Geçiş: geri sıçrama -> özel poz -> normal duruş -> bağırma (bağırırken maske ve gözler void'e döner).
        private IEnumerator VoidTransition(GameObject pv, string why)
        {
            if (pv == null || _voidTransitionRunning) yield break;
            _voidTransitionRunning = true;
            PlayMakerFSM m = MainFsm(pv);
            HealthManager hm = pv.GetComponent<HealthManager>();
            tk2dSpriteAnimator anim = pv.GetComponent<tk2dSpriteAnimator>();
            tk2dSprite sp = pv.GetComponent<tk2dSprite>();
            if (m == null || hm == null || anim == null || sp == null) { _voidTransitionRunning = false; yield break; }

            // Uygun anı bekle: yerde, boşta (stun'dan kalkmış, ceza bitmiş).
            float timeout = Time.time + 20f;
            while (pv != null && Time.time < timeout && (_pvPunishRunning || m.ActiveStateName != "Idle")) yield return null;
            if (pv == null) { _voidTransitionRunning = false; yield break; }

            // Pure Vessel'in bütün davranışı dursun: durum makineleri kapanır, animasyonu biz oynatırız.
            var paused = new System.Collections.Generic.List<PlayMakerFSM>();
            foreach (PlayMakerFSM f in pv.GetComponents<PlayMakerFSM>()) if (f.enabled) { f.enabled = false; paused.Add(f); }
            Rigidbody2D rb = pv.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            // Geçiş boyunca iki taraf da hasar almaz.
            _noHitBoss = hm;
            bool prevInv = PlayerData.instance.isInvincible;
            PlayerData.instance.isInvincible = true;

            // 1) Geri sıçrama: Knight'tan uzağa (oyunun kendi geri sıçrama animasyonu).
            HeroController hero = HeroController.instance;
            float away = hero != null && hero.transform.position.x > pv.transform.position.x ? -1f : 1f;
            var vl = m.FsmVariables.FindFsmFloat("Left X"); var vr = m.FsmVariables.FindFsmFloat("Right X");
            float minX = vl != null ? vl.Value + 1.5f : float.MinValue, maxX = vr != null ? vr.Value - 1.5f : float.MaxValue;
            float ta = PlayStateClip(anim, m, "Evade Antic", 0.15f);
            float tEnd = Time.time + Mathf.Min(ta, 0.35f);
            while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
            PlayStateClip(anim, m, "Evade", 0.3f);
            tEnd = Time.time + VoidEvadeTime;
            while (pv != null && Time.time < tEnd)
            {
                float x = pv.transform.position.x;
                bool blocked = (away < 0f && x <= minX) || (away > 0f && x >= maxX);
                if (rb != null) rb.velocity = new Vector2(blocked ? 0f : away * VoidEvadeSpeed, rb.velocity.y);
                yield return null;
            }
            if (pv != null)
            {
                float tr = PlayStateClip(anim, m, "EvadeRecover", 0.2f);
                tEnd = Time.time + Mathf.Min(tr, 0.4f);
                while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
            }

            // 2) Özel poz: oyunda kullanılmayan "selam" animasyonu, son karede bir süre durur.
            tk2dSpriteAnimationClip pose = pv != null ? MakePoseClip(sp.Collection) : null;
            if (pose != null)
            {
                anim.Play(pose);
                tEnd = Time.time + pose.frames.Length / pose.fps + VoidPoseHold;
                while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
                // Pozdan normal duruşa hızlıca geri dön (aynı kareler tersten).
                tk2dSpriteAnimationClip back = MakePoseBackClip(pose);
                if (pv != null && back != null)
                {
                    anim.Play(back);
                    tEnd = Time.time + back.frames.Length / back.fps;
                    while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
                }
            }
            else if (pv != null) { }

            // 3) Normal duruş, sonra bağırma.
            if (pv != null) yield return IdleFor(pv, anim, rb, VoidIdleBeforeRoar);

            // 4) Bağırma hazırlığı (oyunun "Roar Antic" animasyonu), sonra bağırma. Knight bağırma başlarken kilitlenir.
            bool roarLocked = false, heroLocked = false;
            GameObject roarWave = null;
            if (pv != null)
            {
                float tra = PlayStateClipByName(anim, "Roar Antic", 0.5f);
                tEnd = Time.time + tra;
                while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
            }
            if (pv != null)
            {
                roarLocked = RoarLock(true, pv);
                if (!roarLocked && hero != null)
                {
                    try { hero.RelinquishControl(); hero.StopAnimationControl(); heroLocked = true; } catch (System.Exception) { }
                }
                float roarLen = Mathf.Max(VoidMinRoar, PlayStateClip(anim, m, "Intro Roar", VoidMinRoar)) + VoidRoarExtra;
                RunNativeActions(m, "Intro Roar");
                var roarAudio = StoredAudioPlayers(m, "Intro Roar");
                try { GameCameras.instance.cameraShakeFSM.SendEvent("BigShake"); } catch (System.Exception) { }
                // Savaş başındaki gibi: bağırma sesi animasyon bitmeden solarak biter.
                GameManager.instance.StartCoroutine(FadeRoarAudio(roarAudio, roarLen - VoidRoarFadeEarly - VoidRoarFade, VoidRoarFade));
                // Savaş başındaki bağırmanın dalga görüntüsü: oyunun kendi "Roar Wave Emitter"ı.
                roarWave = PlayNativeRoarVisual(pv, m);
                if (roarWave == null) GameManager.instance.StartCoroutine(RoarWaves(pv, roarLen));
                VoidSkin vs = AddVoidSkin(pv, 0f);
                RoarFramesPlayer rfp = StartRoarFrames(pv, m, anim, sp);
                float t0 = Time.time;
                while (pv != null && Time.time - t0 < roarLen)
                {
                    float k = Mathf.Clamp01((Time.time - t0) / roarLen);
                    if (rfp != null) { TickRoarFrames(rfp, k); if (vs != null) vs.alpha = 0f; }
                    else if (vs != null) vs.alpha = k * k * (3f - 2f * k);
                    HoldStill(rb);
                    yield return null;
                }
                if (vs != null) vs.alpha = 1f;
                StopRoarFrames(rfp);
            }
            // 5) Bağırma biter: dalga durur, toparlanma animasyonu (savaş başındaki "Recover"), Knight serbest.
            if (roarWave != null)
            {
                foreach (PlayMakerFSM f in roarWave.GetComponentsInChildren<PlayMakerFSM>(true)) { try { f.SendEvent("END"); } catch (System.Exception) { } }
                UnityEngine.Object.Destroy(roarWave, 3f);
            }
            if (roarLocked) RoarLock(false);
            if (heroLocked && hero != null) { try { hero.StartAnimationControl(); hero.RegainControl(); } catch (System.Exception) { } }
            if (pv != null)
            {
                // Toparlanma animasyonu sonuna kadar, sonra normal duruşta biraz bekler; ancak ondan sonra saldırır.
                float trc = PlayStateClipByName(anim, "Recover", 0.5f);
                tEnd = Time.time + Mathf.Clamp(trc, 0.3f, 1.5f);
                while (pv != null && Time.time < tEnd) { HoldStill(rb); yield return null; }
                if (pv != null) yield return IdleFor(pv, anim, rb, VoidIdleAfter);
            }
            PlayerData.instance.isInvincible = prevInv;

            if (pv == null) { _voidTransitionRunning = false; yield break; }

            // 5) Void formu güçleri
            hm.hp += VoidExtraHp;
            // Yukarıdan saplama saldırısı (ve dikenler/tendril'ler) normal hızda kalır; diğer her şey %20 hızlı.
            var allStates = new System.Collections.Generic.List<string>();
            var keepClips = new System.Collections.Generic.HashSet<string>();
            foreach (var st in m.FsmStates)
            {
                if (IsStompChainState(st.Name)) { string c = GetStateClip(m, st.Name); if (c != null) keepClips.Add(c); }
                else allStates.Add(st.Name);
            }
            ScaleWaits(m, allStates.ToArray(), 1f / VoidSpeedMul);
            SpeedUpAnimations(pv, VoidSpeedMul, keepClips);
            foreach (PlayMakerFSM f in pv.GetComponents<PlayMakerFSM>())
            {
                if (f.FsmName != "Stun Control") continue;
                var c1 = f.FsmVariables.FindFsmInt("Stun Combo"); var c2 = f.FsmVariables.FindFsmInt("Stun Hit Max");
                if (c1 != null) c1.Value = Mathf.CeilToInt(c1.Value * VoidStunMul);
                if (c2 != null) c2.Value = Mathf.CeilToInt(c2.Value * VoidStunMul);
            }
            MuteHitEffects(hm);

            // 6) Bırak: durum makineleri tekrar çalışır, boştan başlayıp yeni saldırısını seçer.
            foreach (PlayMakerFSM f in paused) if (f != null) f.enabled = true;
            if (_noHitBoss == hm) _noHitBoss = null;
            m.SetState("Idle");
            _pvPrevState = "Idle";
            _voidFormDone = true;
            _voidTransitionRunning = false;
        }


        // Oyunun kendi bağırma dalgasını oynatır: bağırma adımlarındaki nesne oluşturma/açma eylemleri ve
        // Pure Vessel'in altındaki "Roar" adlı efekt nesneleri. Bir şey bulduysa true.
        private GameObject PlayNativeRoarVisual(GameObject pv, PlayMakerFSM m)
        {
            foreach (var st in m.FsmStates)
            {
                if (st.Name != "Intro Roar" || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || a.GetType().Name != "CreateObject") continue;
                    try
                    {
                        a.OnEnter();
                        var so = a.GetType().GetField("storeObject")?.GetValue(a) as HutongGames.PlayMaker.FsmGameObject;
                        GameObject made = so != null ? so.Value : null;
                        return made;
                    }
                    catch (System.Exception) {  }
                }
            }
            return null;
        }

        // Bağırmanın dalga görüntüsü: başından dışa yayılan halkalar (savaş başındaki bağırma dalgaları gibi).
        private const float RoarWaveInterval = 0.28f;
        private const float RoarWaveLife = 0.7f;
        private const float RoarWaveRadius = 16f;
        private IEnumerator RoarWaves(GameObject pv, float duration)
        {
            Shader sd = Shader.Find("Sprites/Default");
            if (sd == null || pv == null) yield break;
            Renderer pr = pv.GetComponent<Renderer>();
            float tEnd = Time.time + duration;
            while (pv != null && Time.time < tEnd)
            {
                Vector3 c = pr != null ? new Vector3(pr.bounds.center.x, pr.bounds.center.y + pr.bounds.extents.y * 0.45f, pv.transform.position.z - 0.05f) : pv.transform.position;
                GameManager.instance.StartCoroutine(OneRoarWave(c, sd, pr));
                yield return new WaitForSeconds(RoarWaveInterval);
            }
        }
        private IEnumerator OneRoarWave(Vector3 c, Shader sd, Renderer pr)
        {
            GameObject g = new GameObject("Parry Roar Wave");
            g.transform.position = c;
            LineRenderer lr = g.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 72;
            lr.material = new Material(sd);
            if (pr != null) { lr.sortingLayerID = pr.sortingLayerID; lr.sortingOrder = pr.sortingOrder + 4; }
            float t0 = Time.time;
            while (Time.time - t0 < RoarWaveLife)
            {
                float k = (Time.time - t0) / RoarWaveLife;
                float r = Mathf.Lerp(1f, RoarWaveRadius, 1f - (1f - k) * (1f - k));
                for (int i = 0; i < 72; i++) { float a = i / 72f * Mathf.PI * 2f; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.85f, 0f)); }
                float w = Mathf.Lerp(0.45f, 0.06f, k);
                lr.startWidth = lr.endWidth = w;
                Color col = new Color(1f, 1f, 1f, 0.75f * (1f - k));
                lr.startColor = lr.endColor = col;
                yield return null;
            }
            UnityEngine.Object.Destroy(g);
        }

        // ---------- Void formundaki Pure Vessel'e vuruş: beyaz efekt yerine kara baloncuklar, soul %25 az ----------
        private const int HitBubbleCount = 28;
        private const float HitBubbleLife = 0.25f;
        private const float VoidSoulMul = 0.75f;   // vuruş başına soul (charm'lar dahil) bu kadarına düşer
        private int _voidHitFrame = -1;

        // Oyunun vuruş efektinde sadece arkadan saçılan beyaz parçacıkları kapatır; parlama ve ses aynen kalır
        // (onların yerine kara baloncuklar çıkar).
        private void MuteHitEffects(HealthManager hm)
        {
            try
            {
                var f = typeof(HealthManager).GetField("hitEffectReceiver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                object rec = f != null ? f.GetValue(hm) : null;
                if (rec == null) {  return; }
                var list = new System.Collections.Generic.List<string>();
                foreach (var fi in rec.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    if (fi.FieldType != typeof(GameObject)) continue;
                    GameObject g = fi.GetValue(rec) as GameObject;
                    bool particles = fi.Name.EndsWith("Pt") || fi.Name.ToLowerInvariant().Contains("hitpt") || fi.Name.ToLowerInvariant().Contains("puff");
                    list.Add(fi.Name + "=" + (g != null ? g.name : "yok") + (particles && g != null ? " (KAPATILDI)" : ""));
                    // Boş bırakınca oyun vuruşu yarıda kesip bir sonraki karede tekrar vuruyordu (çift ses):
                    // yerine hiçbir şey göstermeyen boş bir nesne koyuyoruz.
                    if (particles && g != null) fi.SetValue(rec, DummyHitPt());
                }
            }
            catch (System.Exception) {  }
        }

        // Hiçbir şey göstermeyen, oluşturulunca kendini silen boş efekt.
        private class SelfDestroy : MonoBehaviour
        {
            void Start() { if (gameObject.name.Contains("(Clone)")) Destroy(gameObject, 0.2f); }
        }
        private static GameObject _dummyHitPt;
        private static GameObject DummyHitPt()
        {
            if (_dummyHitPt != null) return _dummyHitPt;
            _dummyHitPt = new GameObject("Parry Empty Hit Pt");
            _dummyHitPt.transform.position = new Vector3(-9999f, -9999f, 0f);
            _dummyHitPt.AddComponent<SelfDestroy>();
            UnityEngine.Object.DontDestroyOnLoad(_dummyHitPt);
            return _dummyHitPt;
        }

        private void VoidHitBubbles(HealthManager hm, HitInstance hit)
        {
            Shader sd = Shader.Find("Sprites/Default");
            if (sd == null) return;
            Renderer pr = hm.GetComponent<Renderer>();
            Collider2D col = hm.GetComponent<Collider2D>();
            Vector3 p = hm.transform.position;
            if (hit.Source != null)
            {
                Vector2 sp = hit.Source.transform.position;
                if (col != null) p = col.bounds.ClosestPoint(sp);
                else p = sp;
            }
            p.z = hm.transform.position.z - 0.02f;
            float dir = hit.Direction * Mathf.Deg2Rad;
            GameObject g = new GameObject("Parry Void Hit");
            g.transform.position = p;
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 100; main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(HitBubbleLife * 0.5f, HitBubbleLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0f, 0f, 0f, 1f), new Color(0.05f, 0.04f, 0.07f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.15f;
            // Her yöne saçılır ama vuruş yönüne doğru ağırlıklı.
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(Mathf.Cos(dir) * 5f, Mathf.Cos(dir) * 9f);
            vel.y = new ParticleSystem.MinMaxCurve(Mathf.Sin(dir) * 5f, Mathf.Sin(dir) * 9f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.25f; lim.limit = 2f;
            ParticleSystemRenderer rr = g.GetComponent<ParticleSystemRenderer>();
            Material mat = new Material(sd); mat.mainTexture = DropTex(); rr.material = mat;
            if (pr != null) { rr.sortingLayerID = pr.sortingLayerID; rr.sortingOrder = pr.sortingOrder + 5; }
            ps.Emit(HitBubbleCount);
            UnityEngine.Object.Destroy(g, HitBubbleLife + 0.3f);
        }

        // Soul kazanımı: void formundaki Pure Vessel'e vurulan karede, charm'lar dahil kazanılan soul %25 azalır.
        private void OnSoulGain(On.HeroController.orig_SoulGain orig, HeroController self)
        {
            if (_voidHitFrame != Time.frameCount) { orig(self); return; }
            PlayerData pd = PlayerData.instance;
            int before = pd.MPCharge + pd.MPReserve;
            orig(self);
            int gained = pd.MPCharge + pd.MPReserve - before;
            int cut = Mathf.RoundToInt(gained * (1f - VoidSoulMul));
            if (cut > 0) self.TakeMP(cut);
        }

        // "HK_Prime_acknowledge0000..." karelerinden bir animasyon kurar (oyunda böyle bir animasyon yok).
        private const float VoidPoseBackFps = 18f;   // pozdan normal duruşa dönüşün hızı (kareler geriye, hızlı)
        private static tk2dSpriteAnimationClip MakePoseBackClip(tk2dSpriteAnimationClip pose)
        {
            if (pose == null || pose.frames == null || pose.frames.Length < 2) return null;
            var fr = new tk2dSpriteAnimationFrame[pose.frames.Length];
            for (int i = 0; i < fr.Length; i++) fr[i] = pose.frames[fr.Length - 1 - i];
            tk2dSpriteAnimationClip c = new tk2dSpriteAnimationClip();
            c.name = "Void Acknowledge Back"; c.fps = VoidPoseBackFps; c.wrapMode = tk2dSpriteAnimationClip.WrapMode.Once;
            c.frames = fr;
            return c;
        }

        private static tk2dSpriteAnimationClip MakePoseClip(tk2dSpriteCollectionData col)
        {
            if (col == null) return null;
            var frames = new System.Collections.Generic.List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < 20; i++)
            {
                int id = col.GetSpriteIdByName(VoidPosePrefix + i.ToString("0000"), -1);
                if (id < 0) break;
                tk2dSpriteAnimationFrame f = new tk2dSpriteAnimationFrame();
                f.spriteCollection = col; f.spriteId = id;
                frames.Add(f);
            }
            if (frames.Count == 0) return null;
            tk2dSpriteAnimationClip c = new tk2dSpriteAnimationClip();
            c.name = "Void Acknowledge"; c.fps = VoidPoseFps; c.wrapMode = tk2dSpriteAnimationClip.WrapMode.Once;
            c.frames = frames.ToArray();
            return c;
        }

        // Bütün animasyonları hızlandırır; Pure Vessel yok olunca (sahne değişince) eski hızlar geri gelir.
        private class AnimSpeedRestore : MonoBehaviour
        {
            public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>> orig;
            void OnDestroy() { if (orig != null) foreach (var kv in orig) if (kv.Key != null) kv.Key.fps = kv.Value; }
        }
        private static bool IsStompChainState(string n)
        {
            return n.StartsWith("Dstab") || n.StartsWith("Dtab") || n.StartsWith("Stomp") || n.StartsWith("Plume") || n == "Burst Pause" || n == "TelePos Dstab";
        }

        private static void SpeedUpAnimations(GameObject pv, float mul, System.Collections.Generic.HashSet<string> keep = null)
        {
            tk2dSpriteAnimator an = pv.GetComponent<tk2dSpriteAnimator>();
            if (an == null || an.Library == null || pv.GetComponent<AnimSpeedRestore>() != null) return;
            var orig = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>>();
            foreach (tk2dSpriteAnimationClip c in an.Library.clips)
            {
                if (c == null) continue;
                string cn = c.name ?? "";
                if ((keep != null && keep.Contains(cn)) || cn.Contains("Plume") || cn.Contains("Stomp") || cn.ToLowerInvariant().Contains("dstab")) continue;
                orig.Add(new System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>(c, c.fps));
                c.fps *= mul;
            }
            pv.AddComponent<AnimSpeedRestore>().orig = orig;
        }

        // Void görünümü: Pure Vessel'in üstüne, her karede aynı şekli void atlasıyla çizen bir kopya.
        // (Oyunun kendi malzemesini değiştirmek tutmadı: oyun her karede geri alıyordu.)
        private class VoidSkin : MonoBehaviour
        {
            // alpha < 1: geçiş sırasında üstte yavaşça beliren kopya (maske dönüşümü için).
            // alpha = 1: kopya kalkar, Pure Vessel doğrudan senin atlasınla çizilir (oyunun kendi parlaması vs. aynen çalışır).
            public float alpha = 1f;
            private Texture voidTex, origTex;
            private GameObject ov;
            private MeshFilter src, dst;
            private MeshRenderer srcR, dstR;
            private Material mat;
            private MaterialPropertyBlock mpb;
            private bool direct;
            public void Init(Texture tex)
            {
                voidTex = tex;
                src = GetComponent<MeshFilter>(); srcR = GetComponent<MeshRenderer>();
                origTex = srcR != null && srcR.sharedMaterial != null ? srcR.sharedMaterial.mainTexture : null;
                Sync();
            }
            void LateUpdate() { Sync(); }
            private void MakeOverlay()
            {
                if (ov != null) return;
                Shader sd = Shader.Find("Sprites/Default");
                ov = new GameObject("PV Void Skin");
                ov.transform.SetParent(transform, false);
                ov.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                dst = ov.AddComponent<MeshFilter>();
                dstR = ov.AddComponent<MeshRenderer>();
                mat = new Material(sd); mat.mainTexture = voidTex;
                dstR.sharedMaterial = mat;
                ov.layer = gameObject.layer;
            }
            private void SetTex(Texture t)
            {
                if (srcR == null || t == null) return;
                if (mpb == null) mpb = new MaterialPropertyBlock();
                srcR.GetPropertyBlock(mpb);       // oyunun koyduğu değerler (parlama vb.) korunur
                mpb.SetTexture("_MainTex", t);
                srcR.SetPropertyBlock(mpb);
            }
            private void Sync()
            {
                if (alpha >= 0.999f)
                {
                    // Doğrudan: her karede yeniden uygula (oyun kendi değerlerini koysa bile resim seninki kalır).
                    SetTex(voidTex);
                    direct = true;
                    if (ov != null) { Destroy(ov); ov = null; }
                    return;
                }
                if (direct) { SetTex(origTex); direct = false; }
                MakeOverlay();
                if (dst.sharedMesh != src.sharedMesh) dst.sharedMesh = src.sharedMesh;
                if (srcR != null)
                {
                    dstR.enabled = srcR.enabled && alpha > 0.001f;
                    dstR.sortingLayerID = srcR.sortingLayerID; dstR.sortingOrder = srcR.sortingOrder + 1;
                }
                mat.color = new Color(1f, 1f, 1f, alpha);
            }
            public void Remove() { if (direct) SetTex(origTex); if (ov != null) Destroy(ov); Destroy(this); }
            void OnDestroy() { if (ov != null) Destroy(ov); }
        }

        private VoidSkin AddVoidSkin(GameObject pv, float alpha)
        {
            if (pv == null) return null;
            VoidSkin vs = pv.GetComponent<VoidSkin>();
            if (vs != null) { vs.alpha = alpha; return vs; }
            if (_voidAtlas == null) {  return null; }
            if (pv.GetComponent<MeshFilter>() == null) return null;
            vs = pv.AddComponent<VoidSkin>();
            vs.alpha = alpha;
            vs.Init(_voidAtlas);
            return vs;
        }


        public override void Initialize()
        {
            On.HeroController.Update += OnHeroUpdate;
            On.HeroController.FixedUpdate += OnHeroFixedUpdate;
            On.HeroController.CanDash += OnCanDash;
            InstallKnockLockHooks();
            On.HeroController.TakeDamage += OnTakeDamage;
            if (_dieHook == null) _dieHook = (orig, self, dir, type, ignoreEvasion) => { OnEnemyDie(self); orig(self, dir, type, ignoreEvasion); };
            On.HealthManager.Die += _dieHook;
            On.HealthManager.Hit += OnEnemyHit;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnDreamImpact;
            On.HeroController.Attack += OnHeroAttack;
            ModHooks.LanguageGetHook += OnLanguageGet;
            ShinobiInit();
            On.HeroController.SoulGain += OnSoulGain;
            // Sahne değişince bossları bulmak için her zaman gerekli (log yazmak ayrıca LogSceneEnemies'e bağlı).
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void Unload()
        {
            On.HeroController.Update -= OnHeroUpdate;
            On.HeroController.FixedUpdate -= OnHeroFixedUpdate;
            On.HeroController.CanDash -= OnCanDash;
            foreach (var h in _lockHooks) h.Dispose();
            _lockHooks.Clear();
            On.HeroController.TakeDamage -= OnTakeDamage;
            if (_dieHook != null) On.HealthManager.Die -= _dieHook;
            On.HealthManager.Hit -= OnEnemyHit;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnDreamImpact;
            On.HeroController.Attack -= OnHeroAttack;
            ModHooks.LanguageGetHook -= OnLanguageGet;
            ShinobiUnload();
            On.HeroController.SoulGain -= OnSoulGain;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        // Her karede çalışır: parry tuşuna basıldı mı diye bakar.
        private void OnHeroUpdate(On.HeroController.orig_Update orig, HeroController self)
        {
            orig(self);

            GodseekerCharmCheck(self);
            DashStopTick(self);
            MantisDashTick(self);
            VoidTick();
            KnockLockTurn(self);
            BigLockInput(self);
            ShinobiDangerTick(self);
            HeroLightTick(self);
            DreamGlowTick(self);


            if (!ParryPressed()) return;
            string blockedBy = ParryBlockedAnim(self);
            if (blockedBy != null) {  return; }
            if (!self.acceptingInput || self.cState.dead || self.cState.transitioning) return;
            if (Time.time < _bigParryLockUntil) return;   // büyük parry'nin kayması ve toparlanması bitmeden parry yok

            if (Time.time >= _nextParryAllowed)
            {
                _windowEnd = Time.time + ParryWindow;
                _windowStartTime = Time.time;
                // Çoklu parry süresindeyken basılırsa bekleme süresi başlamaz: o sırada parry yapabilmek engellenmesin.
                _nextParryAllowed = Time.time < _multiParryUntil ? 0f : Time.time + ParryCooldown;
                StartParryBrake(self);
                GuardJellies(self);
                ParryVisuals.Instance(this).OnPress(self);
                // SARI halka: pencere açık olduğu sürece Knight'ın etrafında kalır, kapanırken söner.
            }
            else
            {
                ParryVisuals.Instance(this).NotePress();
                // KIRMIZI halka: kısa bir uyarı.
            }
        }

        // ---------- Pure Vessel'in arkasındaki ışık ----------
        // Void formunda Pure Vessel arka plandan zor ayrılıyor: Knight'ın arkasındaki ışığın (HeroLight)
        // bir kopyası savaş boyunca Pure Vessel'in arkasına konur ve onu takip eder.
        private const float PvLightScale = 2.2f;   // Knight'ın ışığına göre boyut çarpanı
        private const float PvLightAlpha = 1.0f;   // Knight'ın ışığına göre parlaklık çarpanı
        private GameObject _pvLight;
        private float _pvLightNextCheck;

        private void HeroLightTick(HeroController self)
        {
            if (Time.time < _pvLightNextCheck) return;
            _pvLightNextCheck = Time.time + 0.5f;
            GameObject pv = GameObject.Find("HK Prime");
            if (pv == null || _pvLight != null) return;   // ışık PV'ye bağlı: PV yok olunca o da yok olur
            Transform hl = self.transform.Find("HeroLight");
            if (hl == null) {  _pvLightNextCheck = Time.time + 5f; return; }
            try
            {
                GameObject l = UnityEngine.Object.Instantiate(hl.gameObject);
                l.name = "PV Light (ParryMod)";
                // Sadece görüntü kalsın: kopyadaki durum makineleri vs. Knight'ın ışığı gibi davranmasın.
                foreach (Component c in l.GetComponentsInChildren<Component>(true))
                    if (!(c is Transform) && !(c is SpriteRenderer) && !(c is MeshRenderer) && !(c is MeshFilter))
                        UnityEngine.Object.Destroy(c);
                l.transform.localScale = hl.lossyScale * PvLightScale;
                l.transform.SetParent(pv.transform, true);
                Renderer pr = pv.GetComponent<Renderer>();
                Vector3 center = pr != null ? pr.bounds.center : pv.transform.position;
                float zOff = hl.position.z - self.transform.position.z;   // Knight'ın ışığı gibi arkada dursun
                l.transform.position = new Vector3(center.x, center.y, pv.transform.position.z + zOff);
                SpriteRenderer sr = l.GetComponent<SpriteRenderer>();
                if (sr != null) { Color c = sr.color; c.a = Mathf.Min(1f, c.a * PvLightAlpha); sr.color = c; sr.enabled = true; }
                l.SetActive(true);
                _pvLight = l;
            }
            catch (System.Exception) {  _pvLightNextCheck = Time.time + 5f; }
        }

        // ---------- Parry duraksaması ----------
        // Parry'ye basınca Knight hızla (ama bir anda değil) durur ve pencere boyunca (0.38 sn) yerinde kalır.
        // Havada da aynı: düşüş ve yatay hareket durur, Knight havada asılı kalır. Başarılı parry'de hemen biter (savrulma bozulmasın).
        private const float ParryBrakeTime = 0.1f;   // durma süresi (0'a ne kadar hızlı iner)
        private const bool ParryBrakeInAir = true;   // havada da dursun
        private float _brakeStart = -10f, _brakeEnd = -10f;
        private Vector2 _brakeV;

        private const float ParryBrakeTimeY = 0.04f; // havada dikey durma süresi (aşağı kaymasın diye çok kısa)
        private float _brakeGravity = -1f;
        private bool _brakeGravityOff;

        private void StartParryBrake(HeroController h)
        {
            Rigidbody2D rb = h.GetComponent<Rigidbody2D>();
            // Dash sırasında basıldıysa dash biter, havadaki gibi yavaşlayarak durur.
            if (h.cState.dashing)
            {
                try
                {
                    var mi = typeof(HeroController).GetMethod("FinishedDashing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (mi != null) mi.Invoke(h, null);
                    else h.cState.dashing = false;
                }
                catch (System.Exception) {  }
            }
            _brakeV = rb != null ? rb.velocity : Vector2.zero;
            _brakeStart = Time.time;
            _brakeEnd = _windowEnd;
        }

        private void RestoreBrakeGravity(Rigidbody2D rb)
        {
            if (_brakeGravityOff && rb != null && _brakeGravity >= 0f) rb.gravityScale = _brakeGravity;
            _brakeGravityOff = false;
        }

        private void OnHeroFixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
        {
            BigLockInput(self);   // büyük parry kilidi: yürüme/dönme girdisi yok sayılır
            orig(self);
            LedgeGuard(self);
            WallHoldTick(self);
            Rigidbody2D rb = self.GetComponent<Rigidbody2D>();
            // Başarılı parry (_windowEnd sıfırlanır), hasar alma, dash: duraksama hemen biter.
            if (Time.time >= _brakeEnd || _windowEnd < _brakeEnd - 0.001f) { RestoreBrakeGravity(rb); return; }
            if (self.cState.dead || self.cState.recoiling || self.cState.dashing || self.cState.transitioning) { _brakeEnd = -10f; RestoreBrakeGravity(rb); return; }
            if (rb == null) return;
            float k = Mathf.Clamp01((Time.time - _brakeStart) / ParryBrakeTime);
            float keep = 1f - k * k * (3f - 2f * k);   // yumuşak ama hızlı fren
            float vx = _brakeV.x * keep;
            float vy = rb.velocity.y;
            if (ParryBrakeInAir && !self.cState.onGround)
            {
                // Havada: dikey hareket çok hızlı durur ve yerçekimi kapanır (aşağı kaymaz), sağa sola gibi sabit kalır.
                float ky = Mathf.Clamp01((Time.time - _brakeStart) / ParryBrakeTimeY);
                vy = _brakeV.y * (1f - ky);
                if (!_brakeGravityOff) { _brakeGravity = rb.gravityScale; _brakeGravityOff = true; }
                rb.gravityScale = 0f;
            }
            else RestoreBrakeGravity(rb);
            rb.velocity = new Vector2(vx, vy);
        }



        // Oyun Knight'a hasar vermeye çalıştığında çalışır.
        private void OnTakeDamage(
            On.HeroController.orig_TakeDamage orig,
            HeroController self,
            GameObject go,
            CollisionSide damageSide,
            int damageAmount,
            int hazardType)
        {
            _lastHitTime = Time.time;
            HealthManager lastHm = go != null ? go.GetComponentInParent<HealthManager>() : null;
            _lastHitRoot = lastHm != null ? lastHm.gameObject : null;

            // Elder Hu: halka parry'sinden hemen sonra aynı anda düşen diğer halkalar vuramaz.
            if (Time.time < _huRingShieldUntil && go != null && FindAncestor(go, "ring holder") != null) return;
            // Parry'lediğimiz boss saldırısının devamı (ör. Soul Warrior'ın yere inince yaptığı kesik) kısa süre vurmasın.
            if (Time.time < _bossGraceUntil && _bossGraceRoot != null && go != null && go.transform.IsChildOf(_bossGraceRoot.transform)) return;

            // Sersemlemiş (donmuş) boss hiç vuramaz.
            if (_lastHitRoot != null && _frozen.Contains(_lastHitRoot)) return;

            // Az önce parry'lediğimiz saldırı hâlâ değiyorsa tekrar hasar vermesin.
            GameObject ignoreKey = IgnoreKey(go);
            // Az önce parry'lenen vuruş hâlâ değerken (ör. Husk Dandy'nin uzun süren kesiği) tuşa YENİDEN basıldıysa
            // bu temas yeni bir parry sayılır; yoksa koruma süresi bitince vuruş parry'lenemeden hasar veriyordu.
            bool reParry = ignoreKey != null && _ignored.TryGetValue(ignoreKey, out Vector2 g0) && Time.time < g0.y
                           && Time.time <= _windowEnd && _windowStartTime > g0.x + 0.05f;
            if (!reParry && ignoreKey != null && _ignored.TryGetValue(ignoreKey, out Vector2 g) && Time.time < g.y)
            {
                // Hâlâ temas var: korumayı biraz uzat (toplam ParryGraceMax'ı geçmeden).
                _ignored[ignoreKey] = new Vector2(g.x, Mathf.Min(Time.time + 0.15f, g.x + (_shortGrace.Contains(ignoreKey) ? ShortParryGraceMax : ParryGraceMax)));
                return;
            }

            bool windowOpen = Time.time <= _windowEnd;
            // Bossun max savuran saldırıları (büyük parry): pencere daha dar (HeavyParryWindow).
            if (windowOpen && Time.time - _windowStartTime > HeavyParryWindow && IsHeavyBossAttack(go, self))
            {
                windowOpen = false;
            }
            bool normalAttack = hazardType == 1; // log'a göre normal düşman saldırısı 1; dikenler, asit vb. parry'lenmez
            bool alreadySafe = self.cState.invulnerable; // başka sebeple dokunulmazsak parry harcanmaz
            string attackerInfo = DescribeAttacker(go);
            string lowerInfo = attackerInfo.ToLowerInvariant();

            // Sheo mavi saldırı parry'sinden sonraki ek dokunulmazlık.
            if (Time.time < _extraInvulnUntil) return;

            // Grimm alev izi: dash parry'sinden hemen sonra kısa süre vurmasın.
            if (Time.time < _trailGraceUntil && lowerInfo.Contains("flame trail")) return;

            // Grimm alev izi: parry penceresi açıkken ve Grimm dalış/dash yaparken, altında beliren iz
            // dash'in parry'sini çalmasın (yok sayılır, pencere açık kalır).
            if (windowOpen && lowerInfo.Contains("flame trail") && FindBossControlInStates(GrimmDashStates) != null)
            {
                if (Time.time - _lastGraceLog > 0.5f) {  _lastGraceLog = Time.time; }
                return;
            }

            // Pure Vessel tendril: parry'den sonra saldırıdan kalan parçalar (bossa bağlı olmayanlar ya da
            // adında "tendril" geçenler) kısa süre vurmasın. Mermiler hariç.
            if (Time.time < _tendrilGraceUntil
                && (lowerInfo.Contains("tendril")
                    || (go != null && go.GetComponentInParent<HealthManager>() == null && !lowerInfo.Contains("shot"))))
            {
                if (Time.time - _lastGraceLog > 0.5f) {  _lastGraceLog = Time.time; }
                return;
            }

            string lockedBossState = windowOpen ? (UnparryableBossState(go) ?? AttachedLockedState(go) ?? AirOnlyLockedState(go, self)) : null;

            // Çoklu parry: başarılı bir parry'den sonraki 0.2 sn içinde değen başka (parry'lenebilir) saldırılar da parry'lenir.
            bool spent = SpentProjectile(go);
            if (spent && RecentlyParriedProjectile(go))
            {
                return;   // orig çağrılmaz: hasar yok
            }
            if (Time.time < _multiParryUntil && normalAttack && !spent && !UnparryableHit(go, attackerInfo)
                && (UnparryableBossState(go) ?? AttachedLockedState(go) ?? AirOnlyLockedState(go, self)) == null)
            {
                GameObject mk = IgnoreKey(go);
                if (mk != null) _ignored[mk] = new Vector2(Time.time, Time.time + ParryGrace);
                // Bu sırada yeni basılmış bir pencere açıksa o basış da başarılı sayılır: bekleme süresi sıfırlanır,
                // böylece kombonun sıradaki vuruşu için hemen tekrar basılabilir (Stalking Devout'un 3'lü kesiği).
                if (Time.time <= _windowEnd) { _windowEnd = 0f; _nextParryAllowed = 0f; }
                return;
            }

            if (windowOpen && normalAttack && !alreadySafe && !spent && !UnparryableHit(go, attackerInfo) && lockedBossState == null)
            {
                _windowEnd = 0f; // tek pencere = tek parry
                _nextParryAllowed = 0f; // başarılı parry bekleme süresini sıfırlar, hemen tekrar basılabilir
                if (go != null)
                {
                    if (_ignored.Count > 30) _ignored.Clear();
                    // Parry'lenen vuruş + düşmanın gövdesi korunur. Diğer silah vuruşları korunmaz.
                    _ignored[IgnoreKey(go)] = new Vector2(Time.time, Time.time + ParryGrace);
                    GameObject body = BodyKey(go);
                    if (body != null) _ignored[body] = new Vector2(Time.time, Time.time + ParryGrace);
                    // Elder Hu halkası: halkanın yere çarpma vuruşu (Box Impact) ayrı bir alan, o da korunsun.
                    if (go.transform.parent != null && FindAncestor(go, "ring holder") != null)
                    {
                        // Aynı anda düşen 2-3 halka: bir parry hepsini karşılasın.
                        _huRingShieldUntil = Time.time + HuMultiRingGrace;
                        foreach (DamageHero sib in go.transform.parent.GetComponentsInChildren<DamageHero>(true))
                            if (sib != null && sib.gameObject != go)
                                _ignored[sib.gameObject] = new Vector2(Time.time, Time.time + HuRingGrace);
                    }
                }
                string bossState = GetControlState(go);
                HealthManager parriedHm = go != null ? go.GetComponentInParent<HealthManager>() : null;

                // Vuruş bossa bağlı değilse (ayrı tendril nesnesi) ve sahnedeki boss tendril saldırısındaysa
                // vuruşu o bossa bağla.
                if (parriedHm == null)
                {
                    PlayMakerFSM tc = FindBossControlInStates(TendrilStates);
                    if (tc != null)
                    {
                        parriedHm = tc.GetComponent<HealthManager>();
                        bossState = tc.ActiveStateName;
                    }
                }
                bool parriedIsBoss = IsBoss(parriedHm);
                // Koruma süresi: Pure Vessel üçlüsünde kısa, diğer her şeyde normal.
                if (go != null)
                {
                    GameObject k1 = IgnoreKey(go), k2 = BodyKey(go);
                    if (parriedIsBoss && MatchRule(ShortGraceRules, parriedHm.gameObject.name, bossState))
                    {
                        foreach (GameObject k in new[] { k1, k2 })
                            if (k != null) { _ignored[k] = new Vector2(Time.time, Time.time + ShortParryGrace); _shortGrace.Add(k); }
                    }
                    else { if (k1 != null) _shortGrace.Remove(k1); if (k2 != null) _shortGrace.Remove(k2); }
                }
                bool isTendril = parriedIsBoss && bossState != null
                                 && System.Array.IndexOf(TendrilStates, bossState) >= 0;

                // Savrulma yönü: vuruşun geldiği tarafın tersi.
                bool pushRight = damageSide == CollisionSide.left
                                 || (damageSide != CollisionSide.right && !self.cState.facingRight);
                // Kombo kilidi: aynı bossu kısa süre içinde tekrar parry'lersen yön değişmez.
                // (Boss içinden geçip arkana geçse bile ters tarafa savrulmazsın.)
                if (parriedIsBoss && parriedHm.gameObject == _pushLockBoss
                    && Time.time - _pushLockTime < PushLockTime)
                {
                    pushRight = _pushLockRight;
                }
                // Odaklı dash parry'si: kılıç arkanda kalsa bile her zaman bossun koştuğu yöne, yani
                // bossdan uzağa savrulursun (vuruşun değdiği taraf burada yanıltıcı oluyor).
                if (parriedIsBoss && MatchRule(ClawKnockbackRules, parriedHm.gameObject.name, bossState)
                    && _dashDirRight.TryGetValue(parriedHm.gameObject, out bool dashRight)
                    && _dashDirTime.TryGetValue(parriedHm.gameObject, out float dashT) && Time.time - dashT < 4f)
                {
                    pushRight = dashRight;
                }
                if (parriedIsBoss)
                {
                    _pushLockBoss = parriedHm.gameObject;
                    _pushLockRight = pushRight;
                    _pushLockTime = Time.time;
                }

                // Soul Warrior yukarıdan dash: baktığın yönün tersine savrulursun.
                if (parriedIsBoss && MatchRule(MageKnightStompRules, parriedHm.gameObject.name, bossState))
                {
                    pushRight = !self.cState.facingRight;
                }
                // Yukarıdan aşağı (dalış/saplama) saldırılar: her zaman yüzünün baktığı yönün tersine savrulursun.
                if ((parriedIsBoss && MatchRule(TopDownRules, parriedHm.gameObject.name, bossState)) || ContainsAny(lowerInfo, TopDownNames))
                {
                    pushRight = !self.cState.facingRight;
                }
                _ledgeGuardUntil = Time.time + 1.5f;
                MarkParriedProjectile(go);
                if (parriedIsBoss && parriedHm.gameObject.name == "HK Prime") CountPvParry(parriedHm.gameObject);
                DoParry(self, pushRight, ContainsAny(attackerInfo, HeavyKnockbackNames));
                string bossName = parriedIsBoss ? parriedHm.gameObject.name : null;

                // Grimm dalış/dash parry'si: alev izi kısa süre vuramasın (oradan çıkmana zaman kalsın).
                if (bossName != null && bossName.ToLowerInvariant().Contains("grimm")
                    && System.Array.IndexOf(GrimmDashStates, bossState) >= 0)
                {
                    _trailGraceUntil = Time.time + TrailGraceAfterParry;
                }
                bool isPureVessel = bossName != null && bossName.ToLowerInvariant().Contains("hk prime");
                // Hollow Knight: dash ve üçlü vuruşun iptali Pure Vessel'deki gibi (dikenli ceza ve void formu yok).
                bool isHollowKnight = bossName != null && bossName.ToLowerInvariant().Contains("hollow knight boss");

                // Pure Vessel iptalleri (sadece Pure Vessel'a uygulanır).
                if (BossCancelEnabled && (isPureVessel || isHollowKnight) && (ContainsAny(attackerInfo, BossCancelOnParry)
                    || (bossState != null && System.Array.IndexOf(BossCancelStates, bossState) >= 0)))
                {
                    CancelBossAttack(parriedHm.gameObject);
                }
                // Grimm iptali: ışınlanarak kaybolur.
                else if (BossCancelEnabled && MatchRule(TeleOutCancelRules, bossName, bossState))
                {
                    EvadeCancel(parriedHm.gameObject, bossState, false); // geri kay, sonra ışınlan
                }
                else if (BossCancelEnabled && MatchRule(EvadeFirebatRules, bossName, bossState))
                {
                    EvadeCancel(parriedHm.gameObject, bossState, true);  // geri kay, sonra alev yarasası
                }
                // Parry bossun sersemleme sayacına vuruş olarak eklensin.
                bool soulCharge = parriedIsBoss && MatchRule(SoulChargeRules, bossName, bossState);
                if (soulCharge) HalfStunBar(parriedHm.gameObject);      // Soul Master'ın uçma saldırısı: stun barı %50
                else if (parriedIsBoss)
                    // Büyük parry (max savrulan saldırılar): sersemleme barı normal parry'nin 3 katı dolar.
                    AddParryStun(parriedHm.gameObject, IsBigParryAttack(self, bossName, bossState) ? BigParryStunMul : 1f);

                // Sheo pembe saldırı: anında kısa sersemleme.
                if (parriedIsBoss && MatchRule(ShortStunRules, bossName, bossState))
                {
                    ShortStun(parriedHm.gameObject);
                }

                // Sly / Oro / Mato odaklı dash: kısa donma sersemlemesi.
                if (parriedIsBoss && MatchRule(FreezeStunRules, bossName, bossState))
                {
                    if (bossName.ToLowerInvariant().Contains("sly"))
                        GameManager.instance.StartCoroutine(SlyNativeStun(parriedHm.gameObject, go)); // oyunun kendi sersemlemesi
                    else
                        GameManager.instance.StartCoroutine(FreezeStun(parriedHm.gameObject, go));
                }

                // Sheo mavi saldırı: ek dokunulmazlık (mavi boyalar bossa bağlı değil, adından tanınır).
                if ((parriedIsBoss && MatchRule(ExtraInvulnRules, bossName, bossState))
                    || ContainsAny(attackerInfo, ExtraInvulnNames))
                {
                    _extraInvulnUntil = Time.time + ExtraInvulnTime;
                }

                // Soul Master uçma saldırısı: çok uzağa. Soul Warrior dash'leri: uzağa (yerdeki dash orada kesilir).
                if (soulCharge)
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                    SoulChargeCancel(parriedHm.gameObject, bossState);
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && (MatchRule(MageKnightStompRules, bossName, bossState) || MatchRule(MageKnightDashRules, bossName, bossState)))
                {
                    // Çok savrulma (3.4).
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));
                    if (MatchRule(MageKnightDashRules, bossName, bossState)) MageKnightDashCancel(parriedHm.gameObject, bossState);
                    // Dalışın yere inince yaptığı kesik / dash'in gövdesi seni savrulurken tekrar vurmasın.
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                // Boss dash'i ya da üçlü vuruşu parry'lenince Knight daha uzağa savrulsun (içinde kalmasın).
                else if (parriedIsBoss && MatchRule(ClawKnockbackRules, bossName, bossState))
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                    // Grimm (iki versiyon) matkap dalışı: hemen sersemler, sonra 5 saldırı hızlı ve gözleri kırmızı parlar.
                    if (bossName.ToLowerInvariant().Contains("grimm") && (bossState == "AD Fire" || bossState == "AD Edge"))
                        GrimmDrillStun(parriedHm.gameObject);
                }
                else if (parriedIsBoss && MatchRule(FarKnockbackRules, bossName, bossState))
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));
                    // Traitor Lord yerden dash: durur, biraz geri çekilir, yeni saldırısını seçer.
                    if (MatchRule(TraitorDashRules, bossName, bossState))
                    {
                        GameManager.instance.StartCoroutine(TraitorDashCancel(parriedHm.gameObject, bossState));
                        _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                    }
                }
                else if (parriedIsBoss && MatchRule(HornetDashRules, bossName, bossState))
                {
                    // Hornet yukarıdan / yerden dash: biz hafif savrulalım, o geriye kaçsın.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));
                    GameManager.instance.StartCoroutine(HornetDashCancel(parriedHm.gameObject, bossState));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(MossChargeRules, bossName, bossState))
                {
                    // Massive Moss Charger: biz orta savrulalım, o da vurulunca savrulacağının 2 katı savrulsun.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                    GameManager.instance.StartCoroutine(MossChargerPush(parriedHm.gameObject, self));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(MantisLightRules, bossName, bossState))
                {
                    // Mantis Lord / Sisters of Battle: yandan dash senin hizanda biter, içinde kalmayasın diye çok savrulursun.
                    // Havadaysan savrulma yok, pogo gibi sıçrarsın (DoParry'deki havada parry).
                    if (self.cState.onGround)
                        GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));

                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(MantisMediumRules, bossName, bossState))
                {
                    // Mantis Lord / Sisters of Battle: yukarıdan aşağı saldırı -> orta savrulma, baktığın yönün tersine
                    // (vuruşun değdiği taraftan bağımsız).
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, !self.cState.facingRight, KnockDistMedium, SlashKnockbackSpeed));
                }
                else if (parriedIsBoss && MatchRule(HiveDashRules, bossName, bossState))
                {
                    // Hive Knight dash'i: biz orta savrulalım, o geriye gitsin.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                    GameManager.instance.StartCoroutine(HiveDashCancel(parriedHm.gameObject, bossState));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(HiveSlashRules, bossName, bossState))
                {
                    // Hive Knight iğnesini yarım daire döndürdüğü saldırı: biz hafif savrulalım, o geriye gitsin.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));
                    GameManager.instance.StartCoroutine(HiveDashCancel(parriedHm.gameObject, bossState));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(IKDashRules, bossName, bossState))
                {
                    // Broken Vessel / Lost Kin dash: boss önünde durup 0.7 sn sersemler (yerde de havada da).
                    // Yerdeysen savrulursun: Broken Vessel'de çok, Lost Kin'de büyük parry (max);
                    // havadaysan (zıplayıp attığı dash) pogo gibi sıçrarsın.
                    if (self.cState.onGround)
                    {
                        if (bossName.ToLowerInvariant().Contains("lost kin"))
                            GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                        else
                            GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));
                    }
                    IKDashStun(parriedHm.gameObject, bossState);
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(IKDstabRules, bossName, bossState))
                {
                    // Broken Vessel / Lost Kin üstüne dalış: orta savrulma.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                    // Dalıştan sonra etrafına saçılan enfeksiyonlar savrulma biter bitmez vurmasın: savrulma bittikten sonra 0.3 sn daha dokunulmazlık.
                    _extraInvulnUntil = Mathf.Max(Time.time, _noDashUntil) + IKDstabInvuln;
                }
                else if (parriedIsBoss && MatchRule(CollectorGrabRules, bossName, bossState))
                {
                    // Collector eliyle vurma/yakalama (üzerine atılma): 0.5 sn sersemler.
                    CollectorGrabStun(parriedHm.gameObject, bossState);
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(LancerSwordRules, bossName, bossState))
                {
                    // God Tamer'ın kılıçlı savaşçısı: kılıç saldırıları hafif savurur.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));
                }
                else if (parriedIsBoss && MatchRule(LobsterRollRules, bossName, bossState))
                {
                    // God Tamer'ın büyük yaratığı dönerek geliyor: çok savrulursun, o geriye dönüp toparlanır.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));
                    GameManager.instance.StartCoroutine(LobsterRollCancel(parriedHm.gameObject, bossState));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(LobsterLandRules, bossName, bossState))
                {
                    // Duvardan sekip düştüğü yerde: çok savrulursun, o geri dönmez (zaten duruyor).
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(GruzChargeRules, bossName, bossState))
                {
                    // Gruz Mother hızlı uçma saldırısı: biz orta savrulalım, o geriye gitsin.
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                    GameManager.instance.StartCoroutine(GruzChargeCancel(parriedHm.gameObject));
                    _bossGraceRoot = parriedHm.gameObject; _bossGraceUntil = Time.time + BossGraceTime;
                }
                else if (parriedIsBoss && MatchRule(LightKnockbackRules, bossName, bossState))
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));
                }
                else if (parriedIsBoss && MatchRule(SlashKnockbackRules, bossName, bossState))
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                }
                // Galien'in büyük çekici: senden sekip yukarı fırlar, sen de daha uzağa savrulursun.
                if (lowerInfo.Contains("galien hammer"))
                {
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                    GameObject hammer = FindAncestor(go, "galien hammer");
                    if (hammer != null) GameManager.instance.StartCoroutine(HammerBounce(hammer, self));
                }
                // Xero'nun kılıcı (iğnesi): parry'lenince içinden geçmeden Xero'ya geri döner.
                if (go != null && go.name.StartsWith("Sword ") && UnityEngine.GameObject.Find("Ghost Warrior Xero") != null)
                    XeroSwordReturn(go);
                // Collector'ın yukarıdan düşen kavanozları: orta savrulma (baktığın yönün tersine).
                if (lowerInfo.Contains("spawn jar"))
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed));
                // Üzerine atılan / düşen enfeksiyon mermileri (Nosk, Brooding Mawlek, God Tamer, Broken Vessel...):
                // parry'lenince yok olur, yerde birikinti bırakmaz. (Yerdeki birikintinin kendisi değişmez.)
                if (!lowerInfo.StartsWith("puddle box"))
                {
                    foreach (string inf in InfectionShotNames)
                    {
                        if (!lowerInfo.Contains(inf)) continue;
                        GameObject shot = FindAncestor(go, inf) ?? go;
                        shot.SetActive(false);
                        break;
                    }
                }
                // Uumuu savaşı: denizanasından çıkan küçük top (Lil Jellyfish) parry'lenince geldiği yöne seker,
                // çarptığı yerde patlar; orada düşman varsa 70 hasar.
                if (lowerInfo.Contains("lil jellyfish"))
                {
                    HealthManager lj = go.GetComponentInParent<HealthManager>();
                    GameObject ball = lj != null ? lj.gameObject : (FindAncestor(go, "lil jellyfish") ?? go);
                    if (_jellyBalls.Add(ball)) GameManager.instance.StartCoroutine(JellyBallReturn(ball, self));
                }
                // False Knight / Failed Champion: tavandan düşen toplar parry'lenince kırılır.
                if (lowerInfo.Contains("falling barrel")) BreakBarrel(FindAncestor(go, "falling barrel") ?? go);
                // Mantis Lord'un dönen bıçağı: geldiği yöne düz geri gider, atan mantis hemen sonraki saldırısına geçer.
                if (go != null && go.name.StartsWith("Shot Mantis Lord")) MantisShotReturn(go);
                // Traitor Lord: iki yana gönderdiği uzun dalga parry'lenince ona geri gider (değerse 60 hasar), büyük parry.
                GameObject tallSlash = FindAncestor(go, "mega_mantis_tall_slash");
                if (tallSlash != null)
                {
                    TraitorWaveReturn(tallSlash);
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                }
                // Traitor Lord: fırlattığı enfeksiyonlu orak parry'lenince baktığın yöne gider (değerse 15 hasar), savrulma normal.
                GameObject sickle = FindAncestor(go, "shot traitor lord");
                if (sickle != null) TraitorSickleReturn(sickle, self);
                // Hornet'in uzaktan attığı iğne: parry'lenince hemen geri çekilir.
                if (go != null && go.name.StartsWith("Needle"))
                {
                    GameObject hornet = FindHornet();
                    if (hornet != null) GameManager.instance.StartCoroutine(HornetNeedleReturn(hornet, go));
                }
                // Markoth kalkanı: dönüş yönü tersine çevrilir (iki kalkan varsa ikisi birden).
                if (lowerInfo.Contains("markoth shield")) ReverseMarkothShields(go);

                // Şans eseri büyük parry: parry'lenebilen her saldırıda %3 ihtimalle (zaten büyük olanlar hariç).
                // Önceki savrulmanın yerine büyük savrulma geçer; saldırıya özel diğer etkiler (iptal, sersemleme...) aynen kalır.
                // Bosslarda yok (boss savaşında hiçbir saldırıda, mermiler dahil): sadece normal düşmanlar için.
                bool alreadyBig = soulCharge || (parriedIsBoss && IsBigParryAttack(self, bossName, bossState));
                bool randomBig = false;
                if (!alreadyBig && !parriedIsBoss && UnityEngine.Random.value < RandomBigParryChance && !BossFightActive())
                {
                    randomBig = true;
                    // Sersemleme barı: normal katkı zaten eklendi, büyük parry için kalan 2 kat daha.
                    if (parriedIsBoss) AddParryStun(parriedHm.gameObject, BigParryStunMul - 1f);
                    GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                }

                // Pure Vessel aşağı dalış parry'si: dikenler iki dalga çıkar (ceza).
                if (parriedIsBoss && isPureVessel && (bossState == "Stomp Antic" || bossState == "Stomp Down"))
                    GameManager.instance.StartCoroutine(PvPlumePunish(parriedHm.gameObject, _voidFormDone && _pvWatched == parriedHm.gameObject));

                // Canı yüksek olduğu için boss sayılan normal düşmanlar
                if (parriedIsBoss)
                {
                    GameObject pb = parriedHm.gameObject;
                    if (dandy0(pb) == "gorgeous") GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));   // Gorgeous Husk: hafif
                    else if (FsmByName(pb, "ZombieShieldControl") != null) GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f));   // Great Husk Sentry: hafif
                }

                // Grey Prince Zote kılıç kesiği: hemen yere düşer (oyunun kendi sersemlemesi).
                if (parriedIsBoss && bossName.StartsWith("Grey Prince")
                    && (System.Array.IndexOf(GpzSlashStates, bossState) >= 0 || (go != null && go.name.ToLowerInvariant().Contains("slash"))))
                {
                    PlayMakerFSM gc = FsmByName(parriedHm.gameObject, "Control");
                    if (gc != null) { gc.SendEvent("STUN");  }
                }
                // Grey Prince Zote hücumu: arkasına doğru düşer, 1 sn sersemler.
                else if (parriedIsBoss && bossName.StartsWith("Grey Prince") && MatchRule(ClawKnockbackRules, bossName, bossState))
                    GameManager.instance.StartCoroutine(GpzChargeStun(parriedHm.gameObject, self, bossState));

                // Watcher Knight yerde dönerek saldırı: top formundan çıkıp geriye gider.
                if (parriedIsBoss && MatchRule(WatcherRollRules, bossName, bossState))
                    GameManager.instance.StartCoroutine(WatcherRollCancel(parriedHm.gameObject));

                // Flukemarm'ın çıkardığı yaratıklar: bir yere çarpmış gibi geriye sekerler.
                HealthManager flyHm = go != null ? go.GetComponentInParent<HealthManager>() : null;
                if (flyHm != null && flyHm.gameObject.name.StartsWith("Fluke Fly"))
                    GameManager.instance.StartCoroutine(FlukeFlyBounce(flyHm.gameObject, self));
                else if (!parriedIsBoss && NormalEnemyParry(go, self, pushRight, randomBig)) { }   // normal düşmana özel davranış
                else if (InterruptEnemies) InterruptEnemy(go, self);

                // Void tendril: parry'lesen de azaltılmış hasar al (oyunun kendi hasar sistemiyle,
                // böylece hasar sonrası kısa dokunulmazlık da gelir).
                if (isTendril) _tendrilGraceUntil = Time.time + TendrilGraceAfterParry;

                if (isTendril || ContainsAny(attackerInfo, ReducedDamageOnParryNames))
                {
                    int reduced = Mathf.Max(1, damageAmount - TendrilParryDamageReduction);
                    orig(self, go, damageSide, reduced, hazardType);
                    return;
                }
                return; // orig çağrılmadığı için hasar hiç uygulanmaz
            }

            // Test sırasında hangi saldırının hangi isimle geldiğini görmek için:

            orig(self, go, damageSide, damageAmount, hazardType);
        }

        private const float MultiParryTime = 0.2f;   // bir parry'den sonra başka saldırıların da parry'lendiği süre
        private float _multiParryUntil;

        internal float WindowEnd => _windowEnd;

        private void DoParry(HeroController self, bool pushRight, bool heavy)
        {
            _multiParryUntil = Time.time + MultiParryTime;
            ParryVisuals.Instance(this).OnSuccess(self);
            // Havada parry: saldırının özel savrulması yoksa savrulmak yerine pogo yapmış gibi yukarı sıçra.
            // (Özel savrulma varsa onu FarKnockbackRoutine kendisi başlatır; bunu bir kare sonra kontrol ediyoruz.)
            bool onWall = OnWall(self);
            bool inAir = !self.cState.onGround && !onWall;
            if (inAir) GameManager.instance.StartCoroutine(ParryAirBounce(self, _recoilToken, pushRight, heavy));
            if (onWall) StartWallHold(self, NormalRecoilTime(self), "normal parry");
            _noDashUntil = Mathf.Max(_noDashUntil, Time.time + (heavy ? 0.4f : 0.3f));   // savrulma sırasında sadece parry
            // Geri savrulma: Steady Body (charm 14) takılı değilse hafifçe geri it.
            try
            {
                bool steadyBody = PlayerData.instance.GetBool("equippedCharm_14");
                if (inAir || onWall) { /* havada: savrulma yerine pogo; duvarda: olduğu yerde kalır */ }
                else if (heavy)
                {
                    // Ağır savrulma: Steady Body'ye bakmaz, hasar yeme sırasındaki uzun geri savrulmayı kullanır.
                    if (pushRight) self.RecoilRightLong();
                    else self.RecoilLeftLong();
                }
                else if (!steadyBody)
                {
                    if (pushRight) self.RecoilRight();
                    else self.RecoilLeft();
                }
            }
            catch (System.Exception e) { Log("HATA (geri savrulma): " + e); }

            // Geçici görsel geri bildirim. Özel animasyon ve ses sonra eklenebilir.
            try { GameManager.instance.StartCoroutine(HitStopRoutine());  }
            catch (System.Exception e) { Log("HATA (yavaşlama): " + e); }


            try { GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");  }
            catch (System.Exception e) { Log("HATA (sarsıntı): " + e); }

            try { GameManager.instance.StartCoroutine(TintRoutine(self));  }
            catch (System.Exception e) { Log("HATA (mavi ton): " + e); }

            try
            {
                SpriteFlash flash = self.GetComponent<SpriteFlash>();
                if (flash != null) { flash.flashFocusGet();  }

            }
            catch (System.Exception e) { Log("HATA (parlama): " + e); }
        }

        // Gövde çarpışma kutuları ("Colliders" altındakiler ya da düşmanın kendisi) tek bir anahtar sayılır,
        // silah vuruşları (ör. Slash1, Slash2) kendi anahtarlarıyla ayrı takip edilir.
        private static GameObject IgnoreKey(GameObject go)
        {
            if (go == null) return null;
            Transform p = go.transform.parent;
            bool isBody = (p != null && p.name == "Colliders") || go.GetComponent<HealthManager>() != null;
            if (isBody)
            {
                GameObject body = BodyKey(go);
                return body != null ? body : go;
            }
            return go;
        }

        private static GameObject BodyKey(GameObject go)
        {
            if (go == null) return null;
            HealthManager hm = go.GetComponentInParent<HealthManager>();
            return hm != null ? hm.gameObject : null;
        }

        // Düşmanın "Control" durum makinesinin o anki durumu (yoksa null).
        private static string GetControlState(GameObject go)
        {
            GameObject body = BodyKey(go);
            if (body == null) return null;
            PlayMakerFSM m = MainFsm(body);
            return m != null ? m.ActiveStateName : null;
        }

        // Bossun ana durum makinesi. Adı bossa göre değişiyor: Pure Vessel/Grimm/Sly "Control",
        // Oro ve Mato "nailmaster", Sheo "nailmaster_sheo".
        private static readonly string[] MainFsmNames = { "Control", "nailmaster", "nailmaster_sheo", "Black Knight", "Mage Lord", "Mage Lord 2", "Mage Knight", "Big Fly Control", "Mossy Control", "Mantis Lord", "IK Control", "Mimic Spider", "Hornet Nosk", "Mantis", "Beam Miner" };

        private static PlayMakerFSM MainFsm(GameObject body)
        {
            if (body == null) return null;
            foreach (PlayMakerFSM fsm in body.GetComponents<PlayMakerFSM>())
            {
                if (System.Array.IndexOf(MainFsmNames, fsm.FsmName) >= 0) return fsm;
            }
            return null;
        }

        // Bossun yaptığı saldırıyı iptal eder: vuruş kutusunu kapatır, bossu durdurur,
        // "Control" durum makinesini bekleme/toparlanma durumuna geçirir.
        private void CancelBossAttack(GameObject hitbox)
        {
            try
            {
                if (hitbox == null) return;
                HealthManager hm = hitbox.GetComponentInParent<HealthManager>();
                if (hm == null) {  return; }
                GameObject boss = hm.gameObject;

                PlayMakerFSM control = null;
                foreach (PlayMakerFSM fsm in boss.GetComponents<PlayMakerFSM>())
                {
                    if (fsm.FsmName == "Control") { control = fsm; break; }
                }
                if (control == null) {  return; }

                string target = null;
                foreach (string candidate in BossIdleStateCandidates)
                {
                    foreach (var st in control.FsmStates)
                    {
                        if (st.Name == candidate) { target = candidate; break; }
                    }
                    if (target != null) break;
                }

                if (target == null)
                {
                    return;
                }

                string from = control.ActiveStateName;

                // Saldırının KENDİ toparlanma adımına geç (ör. Dash -> Dash Recover). Bu adım duruş
                // animasyonunu oynatır ve saldırının vuruş kutularını kapatır, sonra kendiliğinden
                // Idle Stance'e (bekleme duruşu) geçer. Durum makinesi KAPATILMAZ.
                string recover = target;
                for (int i = 0; i + 1 < BossRecoverMap.Length; i += 2)
                {
                    if (BossRecoverMap[i] == from && HasState(control, BossRecoverMap[i + 1]))
                    {
                        recover = BossRecoverMap[i + 1];
                        break;
                    }
                }

                // Tamamen durdurma, kısa bir kayma ile yavaşlasın.
                Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(rb.velocity.x * 0.3f, rb.velocity.y);

                control.SetState(recover);

                // Tendril: vuruş kutusunu ayrıca kapat (oyun sonraki tendril saldırısında kendisi açar).
                if (System.Array.IndexOf(TendrilStates, from) >= 0)
                {
                    Transform tendrils = boss.transform.Find("Tendrils");
                    if (tendrils != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (Transform c in tendrils)
                        {
                            if (sb.Length > 0) sb.Append(", ");
                            sb.Append(c.name + (c.gameObject.activeSelf ? "(açık)" : "(kapalı)"));
                        }
                    }
                    Transform tHit = boss.transform.Find("Tendrils/T Hit");
                    if (tHit != null) { tHit.gameObject.SetActive(false);  }

                }
                GameManager.instance.StartCoroutine(BossCalmRoutine(boss, control, target));
            }
            catch (System.Exception e) { Log("HATA (boss iptal): " + e); }
        }

        // Knight'ın geri savrulmasını geçici olarak uzatır ve hızlandırır, sonra eski haline getirir.
        // Oyunun kendi savrulma ayarlarını kullanır (RECOIL_HOR_STEPS = süre, RECOIL_HOR_VELOCITY_LONG = hız).
        private int _origRecoilSteps = -1;
        private float _origRecoilSpeed;
        private int _recoilToken;

        private IEnumerator FarKnockbackRoutine(HeroController hc, bool pushRight, float dist, float speedMul)
        {
            // "Max" savrulma = büyük parry (Sekiro gibi): kendi sesi, bol beyaz efekt, sadece Knight yavaşça kayar.
            bool big = dist >= KnockDistMax - 0.01f;
            if (big) {  ParryVisuals.Instance(this).BigParry(hc, BigSlideTime); }
            else if (dist >= KnockDistHigh - 0.01f) ParryVisuals.Instance(this).Burst(hc, false);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic;
            var stepsField = typeof(HeroController).GetField("RECOIL_HOR_STEPS", flags);
            var speedField = typeof(HeroController).GetField("RECOIL_HOR_VELOCITY_LONG", flags);
            if (stepsField == null || speedField == null)
            {
                yield break;
            }

            // Oyunun ASIL değerlerini sadece ilk seferde kaydet. Böylece üst üste parry'lerde
            // zaten büyütülmüş değer tekrar büyütülmez ve sonunda hep asıl değere dönülür.
            // (Alanların türü sürüme göre int ya da float olabiliyor: dönüştürerek oku/yaz.
            //  Eskiden doğrudan çevirmeye çalışınca burada hata verip savrulma hiç uzamıyordu.)
            try
            {
                if (_origRecoilSteps < 0)
                {
                    _origRecoilSteps = System.Convert.ToInt32(stepsField.GetValue(hc));
                    _origRecoilSpeed = System.Convert.ToSingle(speedField.GetValue(hc));
                }
            }
            catch (System.Exception) {  yield break; }
            int origSteps = _origRecoilSteps;
            float origSpeed = _origRecoilSpeed;
            int myToken = ++_recoilToken;

            // Duvardayken savrulma yok: savrulma ne kadar sürecekse Knight o kadar duvarda olduğu yerde durur.
            if (OnWall(hc))
            {
                if (big)
                {
                    StartWallHold(hc, BigSlideTime, "büyük parry");
                    _bigParryLockUntil = _bigLockUntil = Time.time + BigSlideTime * 1.3f + 1f;
                    yield return new WaitForSeconds(BigSlideTime);
                    float recoverTime = ParryVisuals.Instance(this).EndBigHold();
                    _bigLockUntil = _bigParryLockUntil = Time.time + recoverTime;
                }
                else
                {
                    float spd0 = origSpeed * speedMul;
                    int st0 = Mathf.Max(2, Mathf.RoundToInt(dist / Mathf.Max(0.1f, spd0 * Time.fixedDeltaTime)));
                    StartWallHold(hc, st0 * Time.fixedDeltaTime, "savrulma " + dist.ToString("0.0"));
                }
                yield break;
            }

            if (big)
            {
                // Büyük parry: oyun yavaşlamaz, sadece Knight yavaşça geri kayar. Hız baştan yüksek başlayıp
                // BigSlideTime içinde sıfıra iner; gidilen yol ölçülür ve tam 'dist' olunca kayma durdurulur.
                var cancelM = typeof(HeroController).GetMethod("CancelRecoilHorizontal", flags, null, System.Type.EmptyTypes, null);
                float v0 = 2f * dist / BigSlideTime;
                SetNumField(stepsField, hc, 100000);   // biz durduracağız
                SetNumField(speedField, hc, v0);
                try
                {
                    if (cancelM != null) cancelM.Invoke(hc, null);
                    if (pushRight) hc.RecoilRightLong(); else hc.RecoilLeftLong();
                }
                catch (System.Exception) {  }
                _noDashUntil = Time.time + BigSlideTime + 0.1f;
                _bigLockUntil = Time.time + BigSlideTime * 1.3f + 1f;   // kayma bitince aşağıda kesinleşir
                _bigParryLockUntil = _bigLockUntil;
                float x0 = hc.transform.position.x, t = 0f, vSeen = 0f;
                while (hc != null && myToken == _recoilToken && t < BigSlideTime * 1.3f)
                {
                    yield return new WaitForFixedUpdate();
                    t += Time.fixedDeltaTime;
                    float gone = Mathf.Abs(hc.transform.position.x - x0);
                    vSeen = Mathf.Max(vSeen, Mathf.Abs(hc.GetComponent<Rigidbody2D>().velocity.x));
                    if (gone >= dist - 0.02f) break;
                    if (_ledgeBlocked && t >= BigSlideTime) break;
                    if (!hc.cState.recoilingLeft && !hc.cState.recoilingRight) break;
                    float v = Mathf.Max(0.8f, v0 * (1f - t / BigSlideTime));   // yumuşakça yavaşla (en sonda kısa bir an en az 0.8)
                    SetNumField(speedField, hc, v);
                }
                if (hc != null)
                {
                    // Kayma bitti: duruş pozu biter, toparlanma kareleri oynar; kilit onlar bitince açılır.
                    float recoverTime = ParryVisuals.Instance(this).EndBigHold();
                    _bigLockUntil = Time.time + recoverTime;
                    _bigParryLockUntil = _bigLockUntil;
                    _noDashUntil = Mathf.Max(_noDashUntil, _bigLockUntil);
                }
                if (hc != null && myToken == _recoilToken)
                {
                    try { if (cancelM != null && (hc.cState.recoilingLeft || hc.cState.recoilingRight)) cancelM.Invoke(hc, null); } catch (System.Exception) { }
                    try { var rb2 = hc.GetComponent<Rigidbody2D>(); if (rb2 != null && hc.cState.onGround) rb2.velocity = new Vector2(0f, rb2.velocity.y); } catch (System.Exception) { }
                    SetNumField(stepsField, hc, origSteps);
                    SetNumField(speedField, hc, origSpeed);
                }
                yield break;
            }

            // Savrulma mesafesi dash mesafesine göre (dash ≈ 5 birim): hafif 1.6, orta 2.8, çok 4.0, max 5.2.
            float spd = origSpeed * speedMul;
            int steps = Mathf.Max(2, Mathf.RoundToInt(dist / Mathf.Max(0.1f, spd * Time.fixedDeltaTime)));
            float durationMul = steps / (float)Mathf.Max(1, origSteps);   // aşağıdaki bekleme süresi için
            SetNumField(stepsField, hc, steps);
            SetNumField(speedField, hc, spd);
            // Uzun savrulmayı yeni ayarlarla yeniden başlat. (Eskiden parry "ağır" sayılmayan saldırılarda
            // kısa savrulma kullanılıyordu ve bu ayarlar hiç işe yaramıyordu: Watcher Knight'ta 0.7 birimde kalıyordu.)
            try
            {
                // Zaten kısa savrulmadaysa oyun yenisini başlatmaz: önce mevcut savrulmayı bitir.
                var cancel = typeof(HeroController).GetMethod("CancelRecoilHorizontal", flags, null, System.Type.EmptyTypes, null);
                if (cancel != null) cancel.Invoke(hc, null);
                if (pushRight) hc.RecoilRightLong(); else hc.RecoilLeftLong();
            }
            catch (System.Exception) {  }
            _noDashUntil = Time.time + steps * Time.fixedDeltaTime + 0.1f;   // savrulurken dash atılamaz
            // Hafif/orta/çok savrulma: savrulma bitene kadar hiçbir şey yapılamaz (yürüme, dönme, zıplama, saldırı...).
            _bigLockUntil = Mathf.Max(_bigLockUntil, Time.time + steps * Time.fixedDeltaTime + 0.02f);
            Vector3 startPos = hc.transform.position;

            // Savrulmanın bitmesini bekle (uzun savrulmalarda daha uzun).
            float wait = Mathf.Max(0.6f, origSteps * durationMul * Time.fixedDeltaTime + 0.2f);
            yield return new WaitForSeconds(wait);

            // Bu arada yeni bir uzak savrulma başladıysa geri almayı o yapacak.
            if (hc != null && myToken == _recoilToken)
            {
                SetNumField(stepsField, hc, origSteps);
                SetNumField(speedField, hc, origSpeed);
            }
        }

        // Duvarda parry: Knight savrulmaz/sıçramaz, savrulma süresi boyunca duvarda olduğu yerde durur (aşağı kaymaz).
        // Duvarda mı: oyun parry sırasında "wallSliding"i kapatıyor (kayma durunca), ama Knight hâlâ duvara değiyor.
        internal static bool OnWall(HeroController h)
        {
            return h != null && !h.cState.onGround && (h.cState.wallSliding || h.cState.touchingWall);
        }
        private float _wallHoldUntil;
        private Vector3 _wallHoldPos;
        private void StartWallHold(HeroController hc, float duration, string why)
        {
            duration = Mathf.Max(0.1f, duration);
            _wallHoldUntil = Mathf.Max(_wallHoldUntil, Time.time + duration);
            _wallHoldPos = hc.transform.position;
            _bigLockUntil = Mathf.Max(_bigLockUntil, _wallHoldUntil);
            _noDashUntil = Mathf.Max(_noDashUntil, _wallHoldUntil);
            try
            {
                var cancel = typeof(HeroController).GetMethod("CancelRecoilHorizontal",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                if (cancel != null && (hc.cState.recoilingLeft || hc.cState.recoilingRight)) cancel.Invoke(hc, null);
            }
            catch (System.Exception) { }
        }
        private void WallHoldTick(HeroController hc)
        {
            if (hc == null || Time.time >= _wallHoldUntil) return;
            Rigidbody2D rb = hc.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            hc.transform.position = new Vector3(_wallHoldPos.x, _wallHoldPos.y, hc.transform.position.z);
        }
        private float NormalRecoilTime(HeroController hc)
        {
            int steps = _origRecoilSteps;
            if (steps < 0)
            {
                try
                {
                    var f = typeof(HeroController).GetField("RECOIL_HOR_STEPS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (f != null) steps = System.Convert.ToInt32(f.GetValue(hc));
                }
                catch (System.Exception) { }
            }
            return Mathf.Max(0.15f, (steps > 0 ? steps : 4) * Time.fixedDeltaTime);
        }

        // Yerdeyken parry savrulması Knight'ı platformun kenarından düşürmesin: önünde zemin bitince savrulma durur.
        private float _ledgeGuardUntil;
        private float _ledgeLoggedAt = -10f;
        // Çarpmış ve dağılmakta olan mermiler (ör. Soul Warrior'ın ruh topu 'Impact' / 'Impact pause'):
        // görüntüsü sönmüş ya da Knight'ın üstünde dağılıyor, parry'lenemez (yoksa "saldırı yokken" parry oluyordu).
        // Parry'lenen mermiler (düşman değil): kısa süre içinde dağılırken Knight'a değerse hasar vermez.
        private readonly System.Collections.Generic.Dictionary<Transform, float> _parriedProj = new System.Collections.Generic.Dictionary<Transform, float>();
        private const float ParriedProjectileGrace = 1.5f;
        private void MarkParriedProjectile(GameObject go)
        {
            if (go == null || go.GetComponentInParent<HealthManager>() != null) return;
            _parriedProj[go.transform] = Time.time;
        }
        private bool RecentlyParriedProjectile(GameObject go)
        {
            if (go == null) return false;
            float t;
            return _parriedProj.TryGetValue(go.transform, out t) && Time.time - t < ParriedProjectileGrace;
        }

        // Mermi ilk değdiğinde parry'lenebilir (bazı mermiler değdiği an 'Impact' durumuna geçiyor, ör. Soul Warrior'ın topu).
        // Aynı mermi daha önce değmişse ya da görünmüyorsa, Knight'ın üstünde dağılıyor demektir: parry yok.
        private readonly System.Collections.Generic.Dictionary<Transform, float> _projContact = new System.Collections.Generic.Dictionary<Transform, float>();
        private bool SpentProjectile(GameObject go)
        {
            try
            {
                if (go == null || go.GetComponentInParent<HealthManager>() != null) return false;   // düşmanlar değil, sadece mermiler
                Transform key = go.transform;
                float t;
                bool touchedBefore = _projContact.TryGetValue(key, out t) && Time.time - t < 1.5f;
                _projContact[key] = Time.time;
                if (touchedBefore) return true;
                // Görünmüyor ve dağılıyorsa (görüntüsü sönmüş)
                bool visible = false;
                Transform body = go.transform.parent != null ? go.transform.parent : go.transform;
                foreach (Renderer r in body.GetComponentsInChildren<Renderer>())
                {
                    if (r == null || r is ParticleSystemRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                    if (r is SpriteRenderer sr && sr.color.a < 0.05f) continue;
                    visible = true; break;
                }
                return !visible;
            }
            catch (System.Exception) { }
            return false;
        }



        // Kenarda görünmez duvar: savrulma (ve animasyonu) sürer, sadece Knight kenardan öteye gitmez.
        private bool _ledgeBlocked;
        private float _ledgeX;
        private void LedgeGuard(HeroController hc)
        {
            if (hc == null || Time.time > _ledgeGuardUntil || !hc.cState.onGround || (!hc.cState.recoilingLeft && !hc.cState.recoilingRight))
            { _ledgeBlocked = false; return; }
            Rigidbody2D rb = hc.GetComponent<Rigidbody2D>();
            Collider2D col = hc.GetComponent<Collider2D>();
            if (rb == null || col == null) return;
            if (_ledgeBlocked)
            {
                // Zaten kenardayız: savrulmanın itişini her adımda sıfırla, Knight yerinde kalsın.
                rb.velocity = new Vector2(0f, rb.velocity.y);
                hc.transform.position = new Vector3(_ledgeX, hc.transform.position.y, hc.transform.position.z);
                return;
            }
            if (Mathf.Abs(rb.velocity.x) < 0.01f) return;
            float dir = Mathf.Sign(rb.velocity.x);
            Bounds b = col.bounds;
            float ahead = Mathf.Max(0.25f, Mathf.Abs(rb.velocity.x) * Time.fixedDeltaTime * 2f);
            Vector2 probe = new Vector2(dir > 0 ? b.max.x + ahead : b.min.x - ahead, b.min.y + 0.1f);
            int mask = LayerMask.GetMask("Terrain");
            if (mask == 0) mask = 1 << 8;
            RaycastHit2D hit = Physics2D.Raycast(probe, Vector2.down, 0.9f, mask);
            if (hit.collider != null) return;
            _ledgeBlocked = true;
            _ledgeX = hc.transform.position.x;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            if (Time.time - _ledgeLoggedAt > 1f) { _ledgeLoggedAt = Time.time;  }
        }

        // Savrulma seviyeleri (dash ≈ 5 birim)
        private const float KnockDistLight = 1.6f;    // hafif: Watcher Knight dönerek gelme
        private const float KnockDistMedium = 2.8f;   // orta: Pure Vessel üçlü vuruş, Galien çekici
        private const float KnockDistHigh = 4.0f;     // çok: Sly kombo, Soul Warrior
        private const float BigSlideTime = 0.6f;      // büyük parry'de Knight'ın kayma süresi: hızlı başlar, yumuşakça yavaşlayıp durur
        private const float KnockDistMax = 5.2f;      // max: Soul Master uçma, Sly/Oro/Mato odaklı dash, Pure Vessel dash, Grimm dalış (büyük parry)
        private float _noDashUntil;
        // Savrulma kilidi (hafif, orta, çok, max): savrulma bitene kadar Knight hiç hareket edemez (yürüme, dönme,
        // zıplama, saldırı, dash...); max savrulmada toparlanma animasyonu da bitene kadar. Sadece parry tuşu serbest.
        private float _bigLockUntil;
        private float _bigParryLockUntil;                 // büyük parry: kayma ve toparlanma bitene kadar parry tuşu da kapalı
        private const float RandomBigParryChance = 0f;   // kapalı (eskiden %3) // parry'lenebilen her saldırıda büyük parry ihtimali
        private void BigLockInput(HeroController self)
        {
            if (Time.time >= _bigLockUntil || self == null) return;
            if (_moveInputField == null) _moveInputField = typeof(HeroController).GetField("move_input", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (_moveInputField != null) SetNumField(_moveInputField, self, 0f);
            if (self.cState.dead) _bigLockUntil = 0f;
        }


        // Savrulma sırasında sadece parry yapılabilir: zıplama, saldırı, büyü, can basma, süper dash vs. kapalı.
        // (Sağa sola dönmek serbest; hareketi zaten savrulma kendisi belirliyor.)
        private static readonly string[] KnockLockedChecks =
            { "CanJump", "CanDoubleJump", "CanWallJump", "CanAttack", "CanCast", "CanFocus", "CanNailCharge", "CanSuperDash", "CanDreamNail", "CanWallSlide" };
        private readonly System.Collections.Generic.List<MonoMod.RuntimeDetour.Hook> _lockHooks = new System.Collections.Generic.List<MonoMod.RuntimeDetour.Hook>();
        private void InstallKnockLockHooks()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var to = typeof(ParryMod).GetMethod("KnockLockedCan", flags);
            var found = new System.Collections.Generic.List<string>();
            foreach (string n in KnockLockedChecks)
            {
                var mi = typeof(HeroController).GetMethod(n, flags, null, System.Type.EmptyTypes, null);
                if (mi == null || mi.ReturnType != typeof(bool)) continue;
                try { _lockHooks.Add(new MonoMod.RuntimeDetour.Hook(mi, to, this)); found.Add(n); }
                catch (System.Exception) {  }
            }
        }
        private bool KnockLockedCan(System.Func<HeroController, bool> orig, HeroController self)
        {
            if (Time.time < _noDashUntil || Time.time < _bigLockUntil) return false;
            return orig(self);
        }
        private static System.Reflection.FieldInfo _moveInputField;
        // Savrulurken sağa/sola dönebilsin.
        private void KnockLockTurn(HeroController self)
        {
            if (Time.time >= _noDashUntil || Time.time < _bigLockUntil) return;   // büyük parry'de dönmek de yok
            if (_moveInputField == null) _moveInputField = typeof(HeroController).GetField("move_input", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (_moveInputField == null) return;
            float mv = System.Convert.ToSingle(_moveInputField.GetValue(self));
            if (mv > 0.3f && !self.cState.facingRight) self.FaceRight();
            else if (mv < -0.3f && self.cState.facingRight) self.FaceLeft();
        }

        // Savrulma sırasında dash atılamaz.
        private bool OnCanDash(On.HeroController.orig_CanDash orig, HeroController self)
        {
            if (Time.time < _noDashUntil) return false;
            if (Time.time < _brakeEnd && _windowEnd >= _brakeEnd - 0.001f) return false;   // parry duraksamasında dash yok
            return orig(self);
        }

        private static void SetNumField(System.Reflection.FieldInfo f, object o, float v)
        {
            if (f.FieldType == typeof(int)) f.SetValue(o, Mathf.RoundToInt(v));
            else if (f.FieldType == typeof(float)) f.SetValue(o, v);
            else f.SetValue(o, System.Convert.ChangeType(v, f.FieldType));
        }

        // Başarılı parry'yi bossun "Stun Control" sayacına ParryStunHits kadar vuruş olarak ekler.
        // Haritaya göre: her vuruş "STUN DAMAGE" sinyaliyle Combo Counter ve Hits Total'ı 1 artırır,
        // kontrolü de oyun kendisi yapar. Biz önce (ParryStunHits - 1) ekleyip sonra 1 sinyal göndeririz.
        private float _stunCarry;
        private GameObject _stunCarryBoss;

        private const float BigParryStunMul = 3f;   // büyük parry'de sersemleme barına katkı çarpanı

        // Dar pencereli saldırılar: bosslarda max (5,2) savrulma veren saldırılar (büyük parry).
        private bool IsHeavyBossAttack(GameObject go, HeroController self)
        {
            if (go == null) return false;
            if (FindAncestor(go, "mega_mantis_tall_slash") != null) return true;   // Traitor Lord uzun dalga (büyük parry)
            HealthManager hm = go.GetComponentInParent<HealthManager>();
            if (hm == null || !IsBoss(hm)) return false;
            string n = hm.gameObject.name, st = GetControlState(go);
            if (st == null) return false;
            // Sadece max savrulma (büyük parry) veren saldırılar.
            return MatchRule(SoulChargeRules, n, st) || IsBigParryAttack(self, n, st);
        }

        // Büyük parry (max savrulma) olan boss saldırıları.
        private bool IsBigParryAttack(HeroController self, string bossName, string bossState)
        {
            if (MatchRule(ClawKnockbackRules, bossName, bossState)) return true;
            return bossName != null && bossName.ToLowerInvariant().Contains("lost kin") && self.cState.onGround
                   && MatchRule(IKDashRules, bossName, bossState);
        }

        // Sahnede canlı bir boss var mı (şans eseri büyük parry bosslarda olmasın).
        private bool BossFightActive()
        {
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
                if (hm != null && hm.isActiveAndEnabled && hm.hp > 0 && IsBoss(hm)) return true;
            return false;
        }

        private void AddParryStun(GameObject boss, float mul = 1f)
        {
            try
            {
                if (ParryStunHits <= 0 || boss == null) return;

                PlayMakerFSM stun = null;
                foreach (PlayMakerFSM fsm in boss.GetComponents<PlayMakerFSM>())
                {
                    // Pure Vessel'da "Stun Control", Grimm'de "Stun" adında; içleri aynı.
                    if (fsm.FsmName == "Stun Control" || fsm.FsmName == "Stun") { stun = fsm; break; }
                }
                if (stun == null) {  return; }

                // Sadece sayaç çalışırken ekle (Stop/Reset = sersemleme kapalı ya da boss zaten sersemlemiş).
                string state = stun.ActiveStateName;
                if (state != "Idle" && state != "In Combo")
                {
                    return;
                }

                var combo = stun.FsmVariables.FindFsmInt("Combo Counter");
                var total = stun.FsmVariables.FindFsmInt("Hits Total");
                if (combo == null || total == null)
                {
                    return;
                }

                // Küsuratı biriktir (boss değişirse sıfırla). Biriken 1'i geçince tam vuruş olarak ekle.
                if (_stunCarryBoss != boss) { _stunCarryBoss = boss; _stunCarry = 0f; }
                _stunCarry += ParryStunHits * mul;
                int whole = Mathf.FloorToInt(_stunCarry + 0.0001f);
                _stunCarry -= whole;

                if (whole <= 0)
                {
                    return;
                }

                combo.Value += whole - 1;
                total.Value += whole - 1;
                int totalBefore = total.Value;
                stun.SendEvent("STUN DAMAGE"); // son 1 vuruşu oyun ekler ve sersemleme kontrolünü yapar
                // Sayaç sıfırlandıysa ya da sayma durumundan çıktıysa bu parry bossu sersemletti.
                string after = stun.ActiveStateName;
                if (total.Value < totalBefore || (after != "Idle" && after != "In Combo")) MarkParryStun(boss);

                var comboMax = stun.FsmVariables.FindFsmInt("Stun Combo");
                var totalMax = stun.FsmVariables.FindFsmInt("Stun Hit Max");
            }
            catch (System.Exception e) { Log("HATA (sersemletme): " + e); }
        }

        // Soul Master / Soul Tyrant uçarken (Charge Left/Right) oyun stun sinyalini dinlemiyor:
        // bar dolsa bile saldırıya devam ediyordu. Birkaç kare içinde stun'a geçmediyse oyunun kendi stun adımına sokulur.
        private IEnumerator EnsureMageStun(GameObject boss)
        {
            yield return null; yield return null;
            if (boss == null) yield break;
            PlayMakerFSM m = MainFsm(boss);
            if (m == null || BossIsStunned(boss)) yield break;
            string from = m.ActiveStateName;
            if (!HasState(m, "Stun Init")) {  yield break; }
            Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            m.SetState("Stun Init");
        }

        // Soul Master / Soul Tyrant: stun barını %50 doldurur. Zaten %50 ya da üstündeyse o an stun yer.
        // Bar asla tam dolunun üstüne çıkmaz (oyunun kendi kontrolüyle stun olur, sayaç bozulmaz).
        private void HalfStunBar(GameObject boss)
        {
            try
            {
                PlayMakerFSM stun = null;
                foreach (PlayMakerFSM fsm in boss.GetComponents<PlayMakerFSM>())
                    if (fsm.FsmName == "Stun" || fsm.FsmName == "Stun Control") { stun = fsm; break; }
                if (stun == null) {  return; }
                string st = stun.ActiveStateName;
                if (st != "Idle" && st != "In Combo") {  return; }
                var total = stun.FsmVariables.FindFsmInt("Hits Total");
                var max = stun.FsmVariables.FindFsmInt("Stun Hit Max");
                var combo = stun.FsmVariables.FindFsmInt("Combo Counter");
                var comboMax = stun.FsmVariables.FindFsmInt("Stun Combo");
                if (total == null || max == null || max.Value <= 0) {  return; }
                int half = Mathf.CeilToInt(max.Value * 0.5f);
                bool stunNow = total.Value >= half || total.Value + half >= max.Value;
                if (stunNow)
                {
                    // Son vuruşu oyun ekler: tam dolu olur ve stun yer.
                    total.Value = max.Value - 1;
                    if (combo != null && comboMax != null && combo.Value >= comboMax.Value) combo.Value = comboMax.Value - 1;
                    stun.SendEvent("STUN DAMAGE");
                    MarkParryStun(boss);
                    GameManager.instance.StartCoroutine(EnsureMageStun(boss));
                }
                else
                {
                    total.Value += half;
                }
            }
            catch (System.Exception e) { Log("HATA (Soul Master stun): " + e.Message); }
        }

        // Soul Warrior yerden dash: saldırı orada kesilir (Pure Vessel'in dash'indeki gibi), sonra yeni saldırısını seçer.
        private void MageKnightDashCancel(GameObject boss, string from)
        {
            if (SkipCancelForStun(boss, "Soul Warrior dash iptali")) return;
            PlayMakerFSM m = MainFsm(boss);
            if (m == null) return;
            Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            // Kendisi de geriye savrulur: oyunun kendi geri kaçma hareketi ("Evade"), Knight'tan uzağa.
            // Bitince "Evade Recover" -> boşta, sonra yeni saldırısını seçer.
            var crt = m.FsmVariables.FindFsmFloat("Evade Speed Crt");
            var spd = m.FsmVariables.FindFsmFloat("Evade Speed");
            if (crt != null && spd != null && HasState(m, "Evade"))
            {
                bool heroRight = HeroController.instance != null && HeroController.instance.transform.position.x > boss.transform.position.x;
                crt.Value = Mathf.Abs(spd.Value) * (heroRight ? -1f : 1f);
                m.SetState("Evade");
            }
            else
            {
                // Yedek: "Slash Recover" kesik animasyonunu beklediği için takılıyordu; doğrudan boşta durumuna geç.
                m.SetState("Idle");
            }
        }

        // Soul Master / Soul Tyrant uçma saldırısı parry'lenince durur ve bir sonraki saldırısı için ışınlanır.
        private void SoulChargeCancel(GameObject boss, string from)
        {
            if (SkipCancelForStun(boss, "Soul Master uçma iptali")) return;
            PlayMakerFSM m = MainFsm(boss);
            if (m == null) return;
            Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            m.SetState("Charge Stop");   // oyunun kendi adımı: durur, sonra ışınlanır (Tele Away)
        }

        // İptal edilen saldırı -> o saldırının toparlanma adımı (Pure Vessel haritasından).
        private static readonly string[] BossRecoverMap =
        {
            "Dash",    "Dash Recover",
            "Slash 3", "Slash3 Recover",
            // Void tendril: oyunun kendi kapanış adımına ("Tendril Hit End" vuruş kutusunu kapatır,
            // sonra kendiliğinden Tendril Recover -> bekleme duruşu gelir).
            "Tendril Antic",   "Tendril Hit End",
            "Tendril Burst",   "Tendril Hit End",
            "Tendril Start",   "Tendril Hit End",
            "Tendril Hit",     "Tendril Hit End",
            "T2",              "Tendril Hit End",
            "T3",              "Tendril Hit End",
            "Tendril Hit End", "Tendril Hit End"
        };

        private static bool HasState(PlayMakerFSM fsm, string name)
        {
            foreach (var st in fsm.FsmStates)
            {
                if (st.Name == name) return true;
            }
            return false;
        }

        // İptalden sonraki kısa sürede boss karşı parry'ye (Counter) ya da kaçışa (Evade) geçerse
        // onu tekrar normal bekleme duruşuna alır. Böylece bu sürede sana dönük sadece bekler.
        private static readonly string[] BossCalmBlockedStates =
        {
            "Close React", "Counter Antic", "Counter Dir", "Counter Stance", "Evade Antic", "Evade"
        };

        private IEnumerator BossCalmRoutine(GameObject boss, PlayMakerFSM control, string target)
        {
            float end = Time.time + BossCancelPause;
            while (Time.time < end && boss != null && control != null)
            {
                string s = control.ActiveStateName;
                if (System.Array.IndexOf(BossCalmBlockedStates, s) >= 0)
                {
                    control.SetState(target);
                }
                yield return null;
            }

            if (boss == null || control == null) yield break;

            // Teşhis: boss bir yere takılırsa log'dan görelim.
            yield return new WaitForSeconds(1f);
            if (boss != null && control != null)
                { }
            yield return new WaitForSeconds(2f);
            if (boss != null && control != null)
                { }
        }




        // Saldırıyı yapan düşmanı bulup geri iter ve sersemletmeyi dener.
        private void InterruptEnemy(GameObject attacker, HeroController hero)
        {
            try
            {
                if (attacker == null) return;

                HealthManager hm = attacker.GetComponentInParent<HealthManager>();
                if (hm == null)
                {
                    return;
                }

                string chain = DescribeAttacker(attacker) + " {" + BaseName(hm.gameObject.name) + "}";
                if (!ContainsAny(chain, InterruptNames))
                {
                    return;
                }

                GameObject enemy = hm.gameObject;
                if (IsBoss(hm))
                {
                    return;
                }

                // 0) Koşarak gelen düşmanlarda yarım kalan saldırıyı iptal et: davranışı başa sar.
                if (ContainsAny(chain, ResetOnParryNames))
                {
                    foreach (PlayMakerFSM fsm in enemy.GetComponents<PlayMakerFSM>())
                    {
                        string start = fsm.Fsm.StartState;
                        fsm.SetState(start);
                    }
                }

                // 1) Düşmanı Knight'tan uzağa it (0 = sağ, 2 = sol)
                int direction = enemy.transform.position.x >= hero.transform.position.x ? 0 : 2;
                Recoil recoil = enemy.GetComponent<Recoil>();
                if (recoil != null)
                {
                    recoil.RecoilByDirection(direction, EnemyPushStrength);
                }
                else
                {
                }
            }
            catch (System.Exception e)
            {
                Log("HATA (düşman kesme): " + e);
            }
        }

        private IEnumerator TintRoutine(HeroController hc)
        {
            tk2dSprite sprite = hc.GetComponent<tk2dSprite>();
            if (sprite == null)
            {
                yield break;
            }

            Color original = sprite.color;
            sprite.color = new Color(0.4f, 0.9f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
            sprite.color = original;
        }

        // Oyunu kısa süre neredeyse durdurur (parry'nin "ağırlığını" hissettirir).
        private IEnumerator HitStopRoutine()
        {
            if (_hitStopRunning || Time.timeScale < 0.5f) yield break; // oyun zaten duraklatılmışsa dokunma
            _hitStopRunning = true;
            float before = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(0.1f);
            if (Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = before;
            _hitStopRunning = false;
        }


        // ---------- Pure Vessel diken cezası ----------
        private const float PvWave2MarkDelay = 0.15f; // 1. dalga çıktıktan bu kadar sonra 2. dalga işaretlenir
        private const float PvWave2Gap = 0.3f;
        private const float PvWave2MinHold = 0.3f; // 2. dalga çıktıktan sonra Pure Vessel en az bu kadar pozda kalır        // 1. dalga bittikten sonra 2. dalgaya kadar kaçış süresi
        private bool _pvPunishRunning;
        private bool _plumeInfoLogged;

        private static System.Collections.Generic.List<GameObject> ActivePlumes()
        {
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
                if (t.parent == null && t.name.StartsWith("HK Plume Prime") && t.gameObject.activeInHierarchy) list.Add(t.gameObject);
            return list;
        }


        // Diken henüz patlamamış, uyarı halinde bekliyor mu?
        private static bool PlumeWaiting(GameObject g)
        {
            if (g == null || !g.activeInHierarchy) return false;
            PlayMakerFSM pf = g.GetComponent<PlayMakerFSM>();
            string ps = pf != null ? pf.ActiveStateName : "";
            return ps == "Antic" || ps == "Init" || ps == "Outside Arena?" || ps == "Shift Y";
        }

        // Dikenin uyarı (Antic) adımındaki bekleme süresini değiştirir; eski değeri döndürür (-1: bulunamadı).
        // Bu bekleme dolunca diken kendi kendine patlıyor; 2. dalga dikenleri biz patlatana kadar beklemeli.
        private static float SetAnticWait(GameObject plume, float value)
        {
            PlayMakerFSM pf = plume != null ? plume.GetComponent<PlayMakerFSM>() : null;
            if (pf == null) return -1f;
            foreach (var st in pf.FsmStates)
            {
                if (st.Name != "Antic" || st.Actions == null) continue;
                foreach (var act in st.Actions)
                {
                    var w = act as HutongGames.PlayMaker.Actions.Wait;
                    if (w == null || w.time == null) continue;
                    float old = w.time.Value; w.time.Value = value; return old;
                }
            }
            return -1f;
        }

        private static bool PlumeDone(GameObject g)
        {
            if (g == null || !g.activeInHierarchy) return true;
            PlayMakerFSM pf = g.GetComponent<PlayMakerFSM>();
            string ps = pf != null ? pf.ActiveStateName : "";
            return ps == "Deactivate" || ps == "Recycle" || ps == "End";
        }

        // Bir durumdaki ses eylemlerini (AudioPlayerOneShotSingle) oyunun yaptığı gibi çalar:
        // aynı ses oynatıcı kalıbı, aynı ses, aynı yükseklik ve perde.
        // Bir durumdaki ses ve "nesneyi aç" eylemlerini OYUNUN KENDİ KODUYLA yeniden çalıştırır
        // (aynı ses, aynı ses kanalı/yankı, aynı efekt). Animasyon, olay gönderme, fizik eylemlerine dokunmaz.
        private void RunNativeActions(PlayMakerFSM m, string stateName)
        {
            foreach (var st in m.FsmStates)
            {
                if (st.Name != stateName || st.Actions == null) continue;
                foreach (var act in st.Actions)
                {
                    if (act == null) continue;
                    string n = act.GetType().Name;
                    if (!n.StartsWith("Audio") && n != "ActivateGameObject") continue;
                    try
                    {
                        if (n == "ActivateGameObject")
                        {
                            // Açılan nesne zaten açıksa yeniden başlasın diye önce kapat.
                            var fGo = act.GetType().GetField("gameObject")?.GetValue(act) as HutongGames.PlayMaker.FsmOwnerDefault;
                            GameObject target = fGo != null ? m.Fsm.GetOwnerDefaultTarget(fGo) : null;
                            var fAct = act.GetType().GetField("activate")?.GetValue(act) as HutongGames.PlayMaker.FsmBool;
                            if (target != null && fAct != null && fAct.Value && target.activeSelf) target.SetActive(false);
                        }
                        act.OnEnter();
                        if (n.StartsWith("Audio")) { }
                    }
                    catch (System.Exception) {  }
                }
            }
        }

        private const float PlumeSoundVolume = 0.6f; // 2. dalga diken sesinin yüksekliği (0-1)

        private void PlayStateSounds(PlayMakerFSM m, string stateName, Vector3 pos)
        {
            // Bir durumdaki BÜTÜN ses eylemlerini çalar (tek ses, rastgele ses listesi, kendi ses kaynağı...).
            try
            {
                AudioSource own = m.GetComponent<AudioSource>();
                foreach (var st in m.FsmStates)
                {
                    if (st.Name != stateName || st.Actions == null) continue;
                    foreach (var act in st.Actions)
                    {
                        if (act == null) continue;
                        System.Type t = act.GetType();
                        if (!t.Name.StartsWith("Audio")) continue;
                        var clips = new System.Collections.Generic.List<AudioClip>();
                        GameObject player = null;
                        float vol = 1f, pMin = 1f, pMax = 1f;
                        foreach (var f in t.GetFields())
                        {
                            object v = f.GetValue(act);
                            if (v is HutongGames.PlayMaker.FsmObject fo && fo.Value is AudioClip ac) clips.Add(ac);
                            else if (v is AudioClip[] arr) { foreach (var c in arr) if (c != null) clips.Add(c); }
                            else if (v is HutongGames.PlayMaker.FsmObject[] farr) { foreach (var c in farr) if (c != null && c.Value is AudioClip c2) clips.Add(c2); }
                            else if (f.Name == "audioPlayer" && v is HutongGames.PlayMaker.FsmGameObject fg) player = fg.Value;
                            else if (f.Name == "volume" && v is HutongGames.PlayMaker.FsmFloat fv) vol = fv.Value;
                            else if (f.Name == "pitchMin" && v is HutongGames.PlayMaker.FsmFloat f1) pMin = f1.Value;
                            else if (f.Name == "pitchMax" && v is HutongGames.PlayMaker.FsmFloat f2) pMax = f2.Value;
                        }
                        if (clips.Count == 0) continue;
                        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Count)]; // listeden biri (oyun da rastgele seçer)
                        // Oyunun ses oynatıcı nesnesini havuzdan ALMIYORUZ: önceki sürümde ona verdiğimiz ses
                        // takılı kalıyordu ve oyun o nesneyi başka saldırılarda tekrar kullanınca bu ses çalıyordu.
                        // Bunun yerine kendi geçici ses kaynağımızı, oyunun oynatıcısının ayarlarıyla oluşturuyoruz.
                        AudioSource tmpl = player != null ? player.GetComponent<AudioSource>() : own;
                        GameObject tmp = new GameObject("ParryPlumeSound");
                        tmp.transform.position = pos;
                        AudioSource src = tmp.AddComponent<AudioSource>();
                        // Not: oyunun ses kanalını (mixer) kopyalamak sesi tamamen susturuyor (stun sesinde de aynısı
                        // olmuştu). Uzaklıktan bağımsız çalıp ses yüksekliğini oyunun ses ayarına göre veriyoruz.
                        src.spatialBlend = 0f;
                        src.playOnAwake = false;
                        src.loop = false;
                        src.pitch = UnityEngine.Random.Range(pMin, pMax);
                        src.volume = (vol <= 0f ? 1f : vol) * PlumeSoundVolume * GameSfxScale();
                        src.clip = clip;
                        src.Play();
                        GameManager.instance.StartCoroutine(DestroyAfterRealtime(tmp, clip.length / Mathf.Max(0.1f, src.pitch) + 0.3f));
                    }
                }
            }
            catch (System.Exception) {  }
        }

        // ---------- Void tendril (2. dalga görünümü) ----------
        // Pure Vessel'in tendril saldırısındaki ses eylemlerini (sadece sesleri) oyunun kendi koduyla çalar.
        private static readonly string[] TendrilSoundStates = { "Tendril Start", "T2", "Tendril Burst", "T3", "Tendril Antic" };
        private bool _tendrilSoundsLogged;
        private bool PlayVoidTendrilSound(PlayMakerFSM m)
        {
            if (!_tendrilSoundsLogged)
            {
                _tendrilSoundsLogged = true;
                foreach (var st in m.FsmStates)
                {
                    if (System.Array.IndexOf(TendrilSoundStates, st.Name) < 0 || st.Actions == null) continue;
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var act in st.Actions)
                    {
                        if (act == null || !act.GetType().Name.StartsWith("Audio")) continue;
                        foreach (var f in act.GetType().GetFields())
                        {
                            object v = f.GetValue(act);
                            if (v is HutongGames.PlayMaker.FsmObject fo && fo.Value is AudioClip ac) names.Add(ac.name);
                            else if (v is AudioClip[] arr) foreach (var c in arr) if (c != null) names.Add(c.name);
                        }
                    }
                }
            }
            foreach (string sn in TendrilSoundStates)
            {
                foreach (var st in m.FsmStates)
                {
                    if (st.Name != sn || st.Actions == null) continue;
                    bool played = false;
                    foreach (var act in st.Actions)
                    {
                        if (act == null) continue;
                        string an = act.GetType().Name;
                        // Sadece tek seferlik ses eylemleri (döngüye giren sesler takılı kalmasın).
                        if (an != "AudioPlayerOneShotSingle" && an != "AudioPlayerOneShot" && an != "AudioPlayRandom" && an != "AudioPlaySimple") continue;
                        try { act.OnEnter(); played = true; } catch (System.Exception) {  }
                    }
                    if (played) {  return true; }
                }
            }
            return false;
        }

        // ---------- Zemindeki void ----------
        private const float VoidFloorRise = 3.0f;     // aşağıdan ne kadar yükselerek gelir
        private const float VoidFloorAbove = 0.25f;   // (yedek) zemin bulunamazsa: diken tabanının ne kadar üstü
        private const float VoidFloorBelow = 0.2f;    // void'in görünen üst kenarı, Knight'ın bastığı zeminin ne kadar altında durur
        private const float VoidFloorZ = 0.01f;       // derinlik: zeminin önünde, Knight'ın hemen arkasında
        private const float VoidFloorMoveTime = 0.2f; // çıkış ve iniş süresi (ikisi aynı hızda)
        private bool _pitLogged;
        private GameObject _voidFloor;
        private bool _voidFloorDown;
        private bool _voidFloorUp;

        // Bir resmin gerçekten dolu (saydam olmayan) kısmının en üst kenarı, dünya biriminde.
        // Resimlerin üst kısmı çoğunlukla boşluk olduğu için kutu yerine pikselleri ölçüyoruz.
        private static float OpaqueTopWorld(SpriteRenderer sr, float fallback)
        {
            try
            {
                Sprite sp = sr.sprite; if (sp == null) return fallback;
                Texture tex = sp.texture; Rect r = sp.textureRect;
                int w = Mathf.Max(1, (int)r.width), h = Mathf.Max(1, (int)r.height);
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture prev = RenderTexture.active;
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                Texture2D t2 = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t2.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0); t2.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                Color[] px = t2.GetPixels(); UnityEngine.Object.Destroy(t2);
                int top = -1;
                for (int y = h - 1; y >= 0 && top < 0; y--)
                {
                    int full = 0;
                    for (int x = 0; x < w; x++) if (px[y * w + x].a > 0.5f) full++;
                    if (full > w * 0.5f) top = y;
                }
                if (top < 0) return fallback;
                bool flipY = sr.flipY ^ (sr.transform.lossyScale.y < 0f);
                float frac = (top + 1f) / h;
                return flipY ? sr.bounds.max.y - frac * sr.bounds.size.y : sr.bounds.min.y + frac * sr.bounds.size.y;
            }
            catch (System.Exception e) { Modding.Logger.Log("[ParryMod] Opak ölçüm hatası: " + e.Message); return fallback; }
        }

        // Knight'ın altındaki zeminin yüksekliği.
        private static bool GroundUnderHero(out float y)
        {
            y = 0f;
            HeroController h = HeroController.instance; if (h == null) return false;
            Vector2 o = h.transform.position;
            RaycastHit2D hit = Physics2D.Raycast(o + Vector2.up * 0.5f, Vector2.down, 40f, 1 << 8);
            if (hit.collider == null) return false;
            y = hit.point.y; return true;
        }


        private const bool VoidLiquidBody = false;   // true: Radiance'taki düz kara sıvı gövdesi de görünsün
        private const float VoidSmokeRate = 3.5f;    // birim genişlik başına saniyede duman bulutu
        private const float VoidDropRate = 3f;       // birim genişlik başına saniyede kara damla
        private static Texture2D _bodyTex, _smokeTex, _dropTex;

        // Alt kısmı koyu, üste doğru yumuşakça kaybolan gövde (düz bir sıvı kenarı olmasın).
        private static Texture2D BodyTex()
        {
            if (_bodyTex != null) return _bodyTex;
            _bodyTex = new Texture2D(4, 128, TextureFormat.RGBA32, false); _bodyTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 128; y++)
            {
                float t = y / 127f;                       // 0 = alt, 1 = üst
                float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 1f, t));
                for (int x = 0; x < 4; x++) _bodyTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * 0.97f));
            }
            _bodyTex.Apply(); return _bodyTex;
        }

        // Kabarık duman bulutu: üst üste binen yumuşak lekeler.
        private static Texture2D SmokeTex()
        {
            if (_smokeTex != null) return _smokeTex;
            int n = 64; _smokeTex = new Texture2D(n, n, TextureFormat.RGBA32, false); _smokeTex.wrapMode = TextureWrapMode.Clamp;
            System.Random rnd = new System.Random(7);
            var blobs = new System.Collections.Generic.List<Vector3>();
            blobs.Add(new Vector3(0f, 0f, 0.55f));
            for (int i = 0; i < 7; i++)
            {
                float ang = (float)(rnd.NextDouble() * Mathf.PI * 2), d = 0.2f + (float)rnd.NextDouble() * 0.25f;
                blobs.Add(new Vector3(Mathf.Cos(ang) * d, Mathf.Sin(ang) * d, 0.25f + (float)rnd.NextDouble() * 0.2f));
            }
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float px = (x + 0.5f) / n * 2f - 1f, py = (y + 0.5f) / n * 2f - 1f, a = 0f;
                foreach (Vector3 bl in blobs)
                {
                    float dx = px - bl.x, dy = py - bl.y, r = Mathf.Sqrt(dx * dx + dy * dy) / bl.z;
                    a = Mathf.Max(a, Mathf.Clamp01(1f - r));
                }
                float edge = Mathf.Clamp01(1f - Mathf.Sqrt(px * px + py * py));   // kenarlara taşmasın
                a = Mathf.Clamp01(a * 1.4f) * Mathf.Clamp01(edge * 3f);
                _smokeTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            _smokeTex.Apply(); return _smokeTex;
        }

        // Keskin kenarlı küçük kara damla.
        private static Texture2D DropTex()
        {
            if (_dropTex != null) return _dropTex;
            int n = 32; _dropTex = new Texture2D(n, n, TextureFormat.RGBA32, false); _dropTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                _dropTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((0.95f - r) * 12f)));
            }
            _dropTex.Apply(); return _dropTex;
        }

        private static ParticleSystem MakeParticles(Transform parent, string name, Texture2D tex, float w, float yOff, float boxH,
            float rate, Vector2 life, Vector2 size, Color c1, Color c2, Vector2 velY, float velX, float spin, float sizeEnd,
            string layer, int order)
        {
            GameObject pg = new GameObject(name);
            pg.transform.SetParent(parent, false);
            pg.transform.localPosition = new Vector3(0f, yOff, 0f);
            ParticleSystem ps = pg.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = 1500;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var em = ps.emission; em.enabled = true; em.rateOverTime = w * rate;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(w, boxH, 0.01f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-velX, velX);
            vel.y = new ParticleSystem.MinMaxCurve(velY.x, velY.y);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            if (spin > 0f) { var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad); }
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, sizeEnd));
            ParticleSystemRenderer pr = pg.GetComponent<ParticleSystemRenderer>();
            Shader sd = Shader.Find("Sprites/Default");
            if (sd != null) { Material mat = new Material(sd); mat.mainTexture = tex; pr.material = mat; }
            pr.sortingLayerName = layer; pr.sortingOrder = order;
            return ps;
        }

        // Radiance'taki abyss gibi: dibi koyu, üstü dumanlı, içinden kara damlalar yükselen void.
        // surfaceY = görünen üst kenar. Gövde zeminin altında kalır; duman ve damlalar Knight'ın arkasından yükselir.
        private static GameObject MakeVoidFx(float left, float right, float surfaceY, float z, string layer, int order)
        {
            GameObject fx = new GameObject("Parry Void Fx");
            float w = right - left, cx = (left + right) / 2f;
            fx.transform.position = new Vector3(cx, surfaceY, z - 0.001f);

            // Knight'ın çizim katmanı (duman Knight'ın önüne geçip onu örtmesin).
            string heroLayer = "Default"; int heroOrder = 0; float heroZ = 0.004f;
            if (HeroController.instance != null)
            {
                Renderer hr = HeroController.instance.GetComponent<Renderer>();
                if (hr != null) { heroLayer = hr.sortingLayerName; heroOrder = hr.sortingOrder; }
                heroZ = HeroController.instance.transform.position.z;
            }

            // Gövde: zeminin altında tam koyu, üst kenara doğru yumuşakça kaybolur.
            GameObject body = new GameObject("Void Body");
            body.transform.SetParent(fx.transform, false);
            SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = Sprite.Create(BodyTex(), new Rect(0, 0, 4, 128), new Vector2(0.5f, 0f), 128f);
            bsr.color = new Color(0.02f, 0.015f, 0.03f, 1f);
            float bodyH = 9f;   // alttaki %86 tam koyu, üstteki ~1.3 birim yumuşakça kaybolur
            body.transform.localScale = new Vector3(w / (4f / 128f), bodyH, 1f);
            body.transform.localPosition = new Vector3(0f, -bodyH + 0.4f, 0f);   // yumuşak geçişin ortası üst kenara gelsin
            bsr.sortingLayerName = layer; bsr.sortingOrder = order + 1;

            Transform pt = new GameObject("Void Particles").transform;
            pt.SetParent(fx.transform, false);
            pt.position = new Vector3(cx, surfaceY, heroZ + 0.003f);   // Knight'ın hemen arkası

            // Duman bulutları: koyu mor-gri, yavaşça kabarıp yükselir.
            MakeParticles(pt, "Void Smoke Back", SmokeTex(), w, -0.6f, 0.8f, VoidSmokeRate, new Vector2(1.4f, 2.4f), new Vector2(1.6f, 3.4f),
                new Color(0.05f, 0.04f, 0.07f, 0.85f), new Color(0.13f, 0.11f, 0.17f, 0.65f), new Vector2(0.25f, 0.8f), 0.4f, 25f, 1.35f, heroLayer, heroOrder);
            MakeParticles(pt, "Void Smoke Front", SmokeTex(), w, -1.0f, 0.6f, VoidSmokeRate * 0.6f, new Vector2(1.0f, 1.8f), new Vector2(1.2f, 2.6f),
                new Color(0.01f, 0.01f, 0.02f, 0.95f), new Color(0.06f, 0.05f, 0.08f, 0.85f), new Vector2(0.2f, 0.6f), 0.3f, 20f, 1.2f, layer, order + 2);
            // Kara damlalar: küçük, keskin, yükselip küçülerek kaybolur.
            MakeParticles(pt, "Void Drops", DropTex(), w, -0.3f, 0.5f, VoidDropRate, new Vector2(0.8f, 1.8f), new Vector2(0.1f, 0.32f),
                new Color(0f, 0f, 0f, 1f), new Color(0.03f, 0.02f, 0.04f, 1f), new Vector2(0.9f, 2.6f), 0.35f, 0f, 0.25f, heroLayer, heroOrder);
            return fx;
        }

        private static void StartVoidParticles(GameObject fx)
        {
            foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Simulate(1.5f, true, true);   // hazır gelsin (boş başlamasın)
                ps.Play(true);
            }
        }

        private static void SetAlpha(GameObject g, float a)
        {
            foreach (tk2dBaseSprite sp in g.GetComponentsInChildren<tk2dBaseSprite>(true)) { Color c = sp.color; c.a = a; sp.color = c; }
            foreach (SpriteRenderer sr in g.GetComponentsInChildren<SpriteRenderer>(true)) { Color c = sr.color; c.a = a; sr.color = c; }
        }

        // Zemini void ile doldurur: 2. dalga işaretleri konurken yükselir, dalga bitince iner.
        private IEnumerator VoidFloorRoutine(PlayMakerFSM m, float floorY, float z)
        {
            if (_abyssPitPrefab == null) {  yield break; }
            GameObject v = UnityEngine.Object.Instantiate(_abyssPitPrefab);
            // Sadece görüntü kalsın: tendril'ler, durum makineleri, hasar ve çarpışmalar kaldırılır.
            foreach (Transform c in v.transform) if (c.name.StartsWith("Tendrils")) c.gameObject.SetActive(false);
            foreach (PlayMakerFSM f in v.GetComponentsInChildren<PlayMakerFSM>(true)) UnityEngine.Object.Destroy(f);
            foreach (DamageHero dh in v.GetComponentsInChildren<DamageHero>(true)) UnityEngine.Object.Destroy(dh);
            foreach (Collider2D c in v.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (AudioSource a in v.GetComponentsInChildren<AudioSource>(true)) a.enabled = false;
            v.SetActive(true);
            // Radiance sahnesinde parçaları durum makinesi açıyordu; biz açıyoruz (tendril'ler ve çarpışma hariç).
            foreach (Transform c in v.GetComponentsInChildren<Transform>(true))
            {
                if (c == v.transform) continue;
                bool skip = false;
                for (Transform q = c; q != null && q != v.transform; q = q.parent)
                    if (q.name.StartsWith("Tendril") || q.name.StartsWith("Spike Collider")) { skip = true; break; }
                if (skip) continue;
                c.gameObject.SetActive(true);
            }
            foreach (Renderer rd in v.GetComponentsInChildren<Renderer>(true))
            {
                bool tendril = false;
                for (Transform q = rd.transform; q != null && q != v.transform; q = q.parent) if (q.name.StartsWith("Tendril")) tendril = true;
                if (!tendril) rd.enabled = true;
            }
            foreach (Animator an2 in v.GetComponentsInChildren<Animator>(true)) an2.enabled = true;
            yield return null;
            if (!_pitLogged)
            {
                var rl = new System.Collections.Generic.List<string>();
                foreach (Renderer rd in v.GetComponentsInChildren<Renderer>(true))
                {
                    SpriteRenderer sr0 = rd as SpriteRenderer;
                    rl.Add(rd.gameObject.name + "[" + rd.sortingLayerName + ":" + rd.sortingOrder + "]" + (rd.enabled && rd.gameObject.activeInHierarchy ? "" : "(kapalı)")
                        + (sr0 != null && sr0.sprite == null ? "(resim yok)" : "") + " y" + rd.bounds.min.y.ToString("0.0") + ".." + rd.bounds.max.y.ToString("0.0")
                        + " x" + rd.bounds.min.x.ToString("0.0") + ".." + rd.bounds.max.x.ToString("0.0"));
                }
            }

            // Görünür parçaların toplam kutusu.
            Bounds b = new Bounds(v.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer rd in v.GetComponentsInChildren<Renderer>())
            {
                if (!rd.enabled || rd.gameObject.name.StartsWith("Tendril")) continue;
                if (!any) { b = rd.bounds; any = true; } else b.Encapsulate(rd.bounds);
            }
            if (!_pitLogged)
            {
                _pitLogged = true;
                var parts = new System.Collections.Generic.List<string>();
                foreach (Transform t in v.GetComponentsInChildren<Transform>(true))
                {
                    var comps = new System.Collections.Generic.List<string>();
                    foreach (Component c in t.GetComponents<Component>()) if (c != null && !(c is Transform)) comps.Add(c.GetType().Name);
                    parts.Add(t.name + "[" + string.Join(",", comps.ToArray()) + "]");
                }
            }
            if (!any) {  UnityEngine.Object.Destroy(v); yield break; }

            // Fizik: kendi kendine düşmesin.
            Rigidbody2D vrb = v.GetComponent<Rigidbody2D>();
            if (vrb != null) { vrb.isKinematic = true; vrb.velocity = Vector2.zero; vrb.gravityScale = 0f; }

            // Yüzey: "lake_water" parçaları (dalgalanan üst kenar). Parçacık efektleri kutuyu şişirdiği için
            // onlara göre değil, yüzeye göre hizalıyoruz.
            Bounds lake = new Bounds();
            bool hasLake = false;
            foreach (SpriteRenderer sr in v.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.gameObject.name.StartsWith("lake_water") || sr.sprite == null) continue;
                if (!hasLake) { lake = sr.bounds; hasLake = true; } else lake.Encapsulate(sr.bounds);
            }
            if (!hasLake)
            {
                // Yüzey yoksa kara gövdenin üst kenarını kullan.
                foreach (SpriteRenderer sr in v.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.gameObject.name.StartsWith("black_solid") && sr.sprite != null) { lake = sr.bounds; hasLake = true; break; }
                if (!hasLake) lake = b;
            }

            // Arena genişliğinden darsa genişlet (genişse dokunma).
            var vl = m.FsmVariables.FindFsmFloat("Left X");
            var vr = m.FsmVariables.FindFsmFloat("Right X");
            float left = vl != null ? vl.Value - 8f : lake.min.x, right = vr != null ? vr.Value + 8f : lake.max.x;
            // Pure Vessel arenanın kenarındaysa dikenler bu aralığın dışına taşabiliyor: void zemini onları da kapsasın.
            foreach (GameObject pl in ActivePlumes())
            {
                float px = pl.transform.position.x;
                if (px - 4f < left) left = px - 4f;
                if (px + 4f > right) right = px + 4f;
            }
            if (HeroController.instance != null)
            {
                float hx = HeroController.instance.transform.position.x;
                if (hx - 6f < left) left = hx - 6f;
                if (hx + 6f > right) right = hx + 6f;
            }
            if (lake.size.x > 0.1f && lake.size.x < right - left)
            {
                float sx = (right - left) / lake.size.x;
                Vector3 ls = v.transform.localScale;
                v.transform.localScale = new Vector3(ls.x * sx, ls.y, ls.z);
                lake = new Bounds(lake.center, new Vector3(lake.size.x * sx, lake.size.y, lake.size.z));
            }

            Vector3 p0 = v.transform.position;
            // Sıvının asıl seviyesi kara gövdenin üst kenarı; dalga resimleri bunun üstüne taşıyor.
            // (Önceki sürümde dalga resminin en üstünü zemine hizaladığım için her şey zeminin altında kalıyordu.)
            // Görünen üst kenar: kara gövdenin ve dalgaların dolu kısmının en yükseği (piksel ölçümü).
            float blackTop = lake.max.y, waveTop = float.MinValue;
            foreach (SpriteRenderer sr in v.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;
                if (sr.gameObject.name.StartsWith("black_solid")) blackTop = OpaqueTopWorld(sr, sr.bounds.max.y);
                else if (sr.gameObject.name.StartsWith("lake_water") && waveTop == float.MinValue) waveTop = OpaqueTopWorld(sr, sr.bounds.min.y);
            }
            float surfaceTop = Mathf.Max(blackTop, waveTop);
            float ground; bool hasGround = GroundUnderHero(out ground);
            float target = hasGround ? ground - VoidFloorBelow : floorY + VoidFloorAbove;
            float upY = p0.y + (target - surfaceTop);
            floorY = target - VoidFloorAbove;   // efekt (sis/parçacık) aynı kenara otursun
            float downY = upY - VoidFloorRise;
            float cx = (left + right) / 2f;
            v.transform.position = new Vector3(p0.x + (cx - lake.center.x), downY, VoidFloorZ);
            _voidFloor = v;

            // Çizim sırası: dalgalar kara gövdenin, parçacıklar dalgaların önünde olsun.
            string layer = "Over"; int baseOrder = 0;
            foreach (SpriteRenderer sr in v.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.gameObject.name.StartsWith("black_solid")) { layer = sr.sortingLayerName; baseOrder = sr.sortingOrder; }
            foreach (Renderer rd in v.GetComponentsInChildren<Renderer>(true))
            {
                string rn = rd.gameObject.name;
                bool liquid = rn.StartsWith("black_solid") || rn.StartsWith("lake_water") || rn.StartsWith("msk_generic");
                if (liquid && !VoidLiquidBody) { rd.enabled = false; continue; }   // düz sıvı görünümü yok
                if (rn.StartsWith("lake_water")) { rd.sortingLayerName = layer; rd.sortingOrder = baseOrder + 1; }
                else if (rd is ParticleSystemRenderer) { rd.sortingLayerName = layer; rd.sortingOrder = baseOrder + 3; }
            }
            SetAlpha(v, 0f);

            // Kendi efektimiz (yükselen kara parçacıklar + sis), void ile birlikte hareket eder.
            float surfUp = floorY + VoidFloorAbove;
            GameObject fx = MakeVoidFx(left, right, surfUp - VoidFloorRise, VoidFloorZ, layer, baseOrder);
            fx.SetActive(false);
            float fxOff = fx.transform.position.y - downY;

            // Tendril'ler çıkmadan hemen önce (1. dalga bitince) hızla yüksel.
            float timeout = Time.time + 5f;
            while (v != null && !_voidFloorUp && Time.time < timeout) yield return null;
            if (v == null) { if (fx != null) UnityEngine.Object.Destroy(fx); yield break; }
            fx.SetActive(true);
            StartVoidParticles(fx);
            foreach (ParticleSystem ps2 in v.GetComponentsInChildren<ParticleSystem>(true))
            {
                var em2 = ps2.emission; em2.enabled = true;
                ps2.Clear(true); ps2.Simulate(0.8f, true, true); ps2.Play(true);
            }
            float t0 = Time.time;
            while (v != null && Time.time - t0 < VoidFloorMoveTime)
            {
                float k = (Time.time - t0) / VoidFloorMoveTime;
                float e = 1f - (1f - k) * (1f - k);   // hızlı başla, yerine yumuşak otur
                float y = Mathf.Lerp(downY, upY, e);
                v.transform.position = new Vector3(v.transform.position.x, y, v.transform.position.z);
                fx.transform.position = new Vector3(fx.transform.position.x, y + fxOff, fx.transform.position.z);
                SetAlpha(v, Mathf.Clamp01(k * 2f)); SetAlpha(fx, Mathf.Clamp01(k * 2f));
                yield return null;
            }
            if (v != null)
            {
                v.transform.position = new Vector3(v.transform.position.x, upY, v.transform.position.z);
                fx.transform.position = new Vector3(fx.transform.position.x, upY + fxOff, fx.transform.position.z);
                SetAlpha(v, 1f); SetAlpha(fx, 1f);
                int pc = 0; foreach (ParticleSystem ps3 in v.GetComponentsInChildren<ParticleSystem>(true)) pc += ps3.particleCount;
            }

            // 2. dalga bitip tendril'ler geri çekilene kadar bekle.
            timeout = Time.time + 8f;
            while (v != null && !_voidFloorDown && Time.time < timeout) yield return null;

            // Aynı hızla in.
            t0 = Time.time;
            while (v != null && Time.time - t0 < VoidFloorMoveTime)
            {
                float k = (Time.time - t0) / VoidFloorMoveTime;
                float e = k * k;   // yavaş başla, hızlanarak in
                float y = Mathf.Lerp(upY, downY, e);
                v.transform.position = new Vector3(v.transform.position.x, y, v.transform.position.z);
                if (fx != null) { fx.transform.position = new Vector3(fx.transform.position.x, y + fxOff, fx.transform.position.z); SetAlpha(fx, 1f - k); }
                SetAlpha(v, Mathf.Clamp01((1f - k) * 2f));
                yield return null;
            }
            if (v != null) UnityEngine.Object.Destroy(v);
            if (fx != null) UnityEngine.Object.Destroy(fx);
            _voidFloor = null;
        }





        private const float TendrilScaleMul = 1.0f;  // boyut çarpanı: 1 = dikenle aynı boy, 1.3 = %30 daha uzun
        private const float TendrilWidthMul = 1.0f;  // sadece genişlik çarpanı (incelt/kalınlaştır)
        private const int TendrilDamage = 4;         // 2. dalga tendril'lerinin hasarı
        private bool _tendrilSizeLogged;

        // Bir animasyonun en büyük karesinin boyutu (oyun birimi, ölçek 1'de): x = genişlik, y = yükseklik,
        // z = karenin alt kenarının nesnenin merkezine göre yüksekliği.
        private static Vector3 ClipMaxSize(tk2dSpriteAnimator an, string clipName)
        {
            Vector3 best = Vector3.zero;
            if (an == null || an.Library == null) return best;
            tk2dSpriteAnimationClip c = an.Library.GetClipByName(clipName);
            if (c == null || c.frames == null) return best;
            foreach (var fr in c.frames)
            {
                if (fr == null || fr.spriteCollection == null || fr.spriteCollection.spriteDefinitions == null) continue;
                if (fr.spriteId < 0 || fr.spriteId >= fr.spriteCollection.spriteDefinitions.Length) continue;
                var def = fr.spriteCollection.spriteDefinitions[fr.spriteId];
                Vector3[] b = def.untrimmedBoundsData != null && def.untrimmedBoundsData.Length > 1 ? def.untrimmedBoundsData : def.boundsData;
                if (b == null || b.Length < 2) continue;
                if (b[1].y > best.y) best = new Vector3(b[1].x, b[1].y, b[0].y - b[1].y / 2f);
            }
            return best;
        }

        // ---------- Tendril hasar alanı: resmin şeklinden ----------
        // false: tendril'in resminden çıkarılan şekil kullanılmaz; baştan sona pure vessel dikeninin daraltılmış hitbox'ı vurur.
        private const bool UseTendrilShapeHitbox = true;
        private const float TendrilBoxHeight = 0.9f;   // kutunun boyu karenin boyunun ne kadarı (uç biraz affedilir)
        private const int TendrilShapeBands = 40;       // şekil kaç yatay dilimle çıkarılır (fazlası = daha hassas)
        private const float TendrilHitboxShrink = 0.8f; // hasar alanı görüntünün ne kadarı (1 = tam kenar, küçük = daha affedici)
        private static readonly System.Collections.Generic.Dictionary<string, Vector2[]> _tendrilShapes =
            new System.Collections.Generic.Dictionary<string, Vector2[]>();

        private static string ShapeKey(tk2dSpriteCollectionData c, int id) { return (c != null ? c.GetInstanceID() : 0) + ":" + id; }







        private const int ShapeLayer = 31;   // şekil çıkarırken kullanılan, oyunda boş duran katman

        // Her tendril karesini görünmez bir kamerayla gerçekten çizip şeklini çıkarır (ekranda ne görünüyorsa o).
        // Karenin bir kez beyaz, bir kez siyah zemin üstüne çizilir; iki çizimde aynı kalan pikseller doludur.
        private void RenderTendrilShapes()
        {
            GameObject camGo = null, inst = null;
            RenderTexture prev = RenderTexture.active;
            int ok = 0, total = 0;
            try
            {
                camGo = new GameObject("Parry Shape Camera");
                Camera cam = camGo.AddComponent<Camera>();
                cam.enabled = false; cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
                cam.cullingMask = 1 << ShapeLayer; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
                Vector3 origin = new Vector3(8000f, 8000f, 0f);
                foreach (GameObject prefab in _tendrilPrefabs)
                {
                    inst = UnityEngine.Object.Instantiate(prefab);
                    foreach (PlayMakerFSM f in inst.GetComponentsInChildren<PlayMakerFSM>(true)) UnityEngine.Object.Destroy(f);
                    foreach (DamageHero dh in inst.GetComponentsInChildren<DamageHero>(true)) UnityEngine.Object.Destroy(dh);
                    foreach (Collider2D c in inst.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
                    foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ShapeLayer;
                    inst.transform.position = origin; inst.transform.rotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
                    inst.SetActive(true);
                    tk2dSpriteAnimator an = inst.GetComponent<tk2dSpriteAnimator>();
                    tk2dSprite sp = inst.GetComponent<tk2dSprite>();
                    Renderer rd = inst.GetComponent<Renderer>();
                    if (an == null || sp == null || rd == null || an.Library == null) { UnityEngine.Object.Destroy(inst); continue; }
                    an.enabled = false;
                    rd.enabled = true;
                    Color keep = sp.color; sp.color = new Color(keep.r, keep.g, keep.b, 1f);
                    foreach (tk2dSpriteAnimationClip clip in an.Library.clips)
                    {
                        if (clip == null || clip.frames == null) continue;
                        foreach (var fr in clip.frames)
                        {
                            if (fr == null || fr.spriteCollection == null) continue;
                            string key = ShapeKey(fr.spriteCollection, fr.spriteId);
                            if (_tendrilShapes.ContainsKey(key)) continue;
                            total++;
                            sp.SetSprite(fr.spriteCollection, fr.spriteId);
                            Bounds b = rd.bounds;
                            if (b.size.x < 0.01f || b.size.y < 0.01f) { _tendrilShapes[key] = null; continue; }
                            float pad = 0.2f;
                            float fw = b.size.x + pad * 2f, fh = b.size.y + pad * 2f;
                            float ppu = Mathf.Min(48f, 900f / Mathf.Max(fw, fh));
                            int W = Mathf.Max(8, Mathf.CeilToInt(fw * ppu)), H = Mathf.Max(8, Mathf.CeilToInt(fh * ppu));
                            fw = W / ppu; fh = H / ppu;
                            cam.orthographicSize = fh / 2f; cam.aspect = (float)W / H;
                            camGo.transform.position = new Vector3(b.center.x, b.center.y, origin.z - 10f);
                            RenderTexture rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
                            cam.targetTexture = rt;
                            Color[] white = RenderAndRead(cam, rt, W, H, new Color(1f, 1f, 1f, 0f));
                            Color[] black = RenderAndRead(cam, rt, W, H, new Color(0f, 0f, 0f, 0f));
                            cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                            // Yerel koordinata çevir ve dilimle.
                            float left = b.center.x - fw / 2f - origin.x, bottom = b.center.y - fh / 2f - origin.y;
                            var pts = new System.Collections.Generic.List<Vector2>();
                            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                            {
                                Color cw = white[y * W + x], cb = black[y * W + x];
                                float diff = (Mathf.Abs(cw.r - cb.r) + Mathf.Abs(cw.g - cb.g) + Mathf.Abs(cw.b - cb.b)) / 3f;
                                if (diff > 0.5f) continue;   // yarıdan fazla saydam
                                pts.Add(new Vector2(left + (x + 0.5f) / ppu, bottom + (y + 0.5f) / ppu));
                            }
                            Vector2[] poly = BandsToPolygon(pts);
                            _tendrilShapes[key] = poly;
                            if (poly != null) ok++;
                        }
                    }
                    UnityEngine.Object.Destroy(inst); inst = null;
                }
            }
            catch (System.Exception) {  }
            finally
            {
                RenderTexture.active = prev;
                if (inst != null) UnityEngine.Object.Destroy(inst);
                if (camGo != null) UnityEngine.Object.Destroy(camGo);
            }
        }

        private static Color[] RenderAndRead(Camera cam, RenderTexture rt, int W, int H, Color bg)
        {
            cam.backgroundColor = bg;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
            Color[] px = t.GetPixels(); UnityEngine.Object.Destroy(t);
            return px;
        }

        // Dolu noktalardan: her yatay dilimde en sol ve en sağ nokta -> sol kenar yukarı, sağ kenar aşağı çokgen.
        private static Vector2[] BandsToPolygon(System.Collections.Generic.List<Vector2> pts)
        {
            if (pts == null || pts.Count < 3) return null;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 q in pts) { minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y); }
            if (maxY - minY < 0.05f) return null;
            int n = TendrilShapeBands;
            float[] lo = new float[n], hi = new float[n];
            for (int i = 0; i < n; i++) { lo[i] = float.MaxValue; hi[i] = float.MinValue; }
            foreach (Vector2 q in pts)
            {
                int bi = Mathf.Clamp((int)((q.y - minY) / (maxY - minY) * n), 0, n - 1);
                if (q.x < lo[bi]) lo[bi] = q.x;
                if (q.x > hi[bi]) hi[bi] = q.x;
            }
            float bandH = (maxY - minY) / n;
            int last = -1; for (int i = 0; i < n; i++) if (hi[i] >= lo[i]) last = i;
            var left = new System.Collections.Generic.List<Vector2>();
            var right = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                if (hi[i] < lo[i]) continue;
                float c = (lo[i] + hi[i]) / 2f, half = Mathf.Max((hi[i] - lo[i]) / 2f * TendrilHitboxShrink, 0.03f);
                float yA = minY + bandH * i, yB = yA + bandH;
                if (i == last) yB -= bandH * 0.5f;   // uç biraz kısa
                left.Add(new Vector2(c - half, yA)); left.Add(new Vector2(c - half, yB));
                right.Add(new Vector2(c + half, yA)); right.Add(new Vector2(c + half, yB));
            }
            if (left.Count < 2) return null;
            right.Reverse();
            left.AddRange(right);
            return left.ToArray();
        }


        private static Vector2[] CurrentTendrilShape(tk2dSprite sp)
        {
            if (sp == null || sp.Collection == null) return null;
            string key = ShapeKey(sp.Collection, sp.spriteId);
            Vector2[] pts;
            if (_tendrilShapes.TryGetValue(key, out pts)) return pts;
            return null;   // oyun açılışında çıkarılmamış kare: bu karede hitbox kapalı
        }

        // Tendril'in o anki karesine göre hasar alanını günceller.
        private static int UpdateTendrilHitbox(tk2dSprite sp, PolygonCollider2D poly, LineRenderer line, int lastId)
        {
            if (sp == null || poly == null) return lastId;
            if (sp.spriteId == lastId) return lastId;
            Vector2[] pts = CurrentTendrilShape(sp);
            if (pts == null || pts.Length < 3) { poly.enabled = false; if (line != null) line.positionCount = 0; return sp.spriteId; }
            poly.pathCount = 1; poly.SetPath(0, pts); poly.enabled = true;
            if (line != null)
            {
                line.positionCount = pts.Length + 1;
                for (int i = 0; i <= pts.Length; i++) line.SetPosition(i, new Vector3(pts[i % pts.Length].x, pts[i % pts.Length].y, -0.01f));
            }
            return sp.spriteId;
        }

        private int _tendrilsAlive;
        private bool _markersWatch;
        private const float MarkerBubbleRate = 50f;   // uyarı yerinde saniyede çıkan kara baloncuk
        private const float MarkerWidth = 2.0f;       // uyarı sütununun en fazla genişliği (birim); asıl genişlik tendril'in hasar alanı
        private const float MarkerMinWidth = 0.5f;    // uyarı sütununun en az genişliği

        private const float PlumeHitboxWidth = 0.55f;    // ilk 0.3 sn'deki diken hitbox'ının genişliği (1 = dikenin kendisi)
        private struct TendrilPick { public int kind; public float flip; }
        private readonly System.Collections.Generic.Dictionary<GameObject, TendrilPick> _tendrilPicks =
            new System.Collections.Generic.Dictionary<GameObject, TendrilPick>();

        // Her 2. dalga dikeni için hangi tendril'in, hangi yöne dönük çıkacağı (uyarı ile tendril aynı olsun diye önceden seçilir).
        private TendrilPick PickFor(GameObject plume)
        {
            TendrilPick p;
            if (_tendrilPicks.TryGetValue(plume, out p)) return p;
            p.kind = UnityEngine.Random.Range(0, Mathf.Max(1, _tendrilPrefabs.Count));
            p.flip = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _tendrilPicks[plume] = p;
            return p;
        }

        // Tendril'in boyutu, konumu ve zemin hizasında hasar alanının kapladığı yatay aralık (dünya birimi).
        private bool TendrilPlacement(GameObject plume, TendrilPick pick, out float k, out Vector3 pos, out float spanMin, out float spanMax, out Vector3 ps, out Vector3 ts)
        {
            k = 1f; pos = plume.transform.position; spanMin = pos.x - 0.5f; spanMax = pos.x + 0.5f; ps = Vector3.zero; ts = Vector3.zero;
            if (_tendrilPrefabs.Count == 0) return false;
            GameObject prefab = _tendrilPrefabs[pick.kind];
            tk2dSpriteAnimator an = prefab.GetComponent<tk2dSpriteAnimator>();
            string upClip = "Tendril" + (pick.kind + 1) + " Up";
            tk2dSpriteAnimator pan = plume.GetComponent<tk2dSpriteAnimator>();
            ps = ClipMaxSize(pan, "Plume Burst");
            ts = ClipMaxSize(an, upClip);
            Vector3 plScale = plume.transform.lossyScale;
            float plumeH = ps.y * Mathf.Abs(plScale.y);
            k = (ts.y > 0.01f && plumeH > 0.01f) ? plumeH / ts.y * TendrilScaleMul : TendrilScaleMul;
            float plumeBottom = plume.transform.position.y + ps.z * Mathf.Abs(plScale.y);
            Vector3 pp = plume.transform.position;
            pos = new Vector3(pp.x, plumeBottom - ts.z * k, pp.z - 0.01f);

            // Hasar şeklinin (son kare) zemin hizasındaki genişliği.
            try
            {
                tk2dSpriteAnimationClip clip = an != null && an.Library != null ? an.Library.GetClipByName(upClip) : null;
                if (clip == null || clip.frames == null || clip.frames.Length == 0) return true;
                var fr = clip.frames[clip.frames.Length - 1];
                Vector2[] pts;
                if (!_tendrilShapes.TryGetValue(ShapeKey(fr.spriteCollection, fr.spriteId), out pts) || pts == null) return true;
                float gy; if (!GroundUnderHero(out gy)) gy = plumeBottom;
                float y0 = (gy - 0.2f - pos.y) / k, y1 = (gy + 1.5f - pos.y) / k;
                float mn = float.MaxValue, mx = float.MinValue;
                foreach (Vector2 q in pts) if (q.y >= y0 && q.y <= y1) { mn = Mathf.Min(mn, q.x); mx = Mathf.Max(mx, q.x); }
                if (mn > mx) foreach (Vector2 q in pts) { mn = Mathf.Min(mn, q.x); mx = Mathf.Max(mx, q.x); }
                float sx = pick.flip * k * TendrilWidthMul;
                float a = pos.x + sx * mn, b = pos.x + sx * mx;
                spanMin = Mathf.Min(a, b); spanMax = Mathf.Max(a, b);
            }
            catch (System.Exception) { }
            return true;
        }

        // Yeni oluşan 2. dalga dikenlerini yakalar ve her birine baloncuk uyarısı koyar.
        private int _markerGen;
        private IEnumerator WatchWave2Markers(System.Collections.Generic.HashSet<GameObject> wave1Set)
        {
            int gen = ++_markerGen;   // aynı anda iki izleyici çalışmasın (eskisi kendiliğinden durur)
            var done = new System.Collections.Generic.HashSet<GameObject>();
            float timeout = Time.time + 6f;
            while (_markersWatch && gen == _markerGen && Time.time < timeout)
            {
                foreach (GameObject g in ActivePlumes())
                {
                    // Havuzdan yeni çıkan diken ilk karede eski durumunu gösterebiliyor: uyarı haline geçene kadar her karede bak.
                    if (g == null || !PlumeWaiting(g)) continue;   // sadece uyarı halindeki (henüz patlamamış) dikenler
                    if (!done.Add(g)) continue;
                    GameManager.instance.StartCoroutine(BubbleMarker(g));
                }
                yield return null;
            }
            if (gen == _markerGen) _markersWatch = false;
        }

        private readonly System.Collections.Generic.Dictionary<GameObject, float> _anticWaitOrig = new System.Collections.Generic.Dictionary<GameObject, float>();

        private IEnumerator BubbleMarker(GameObject plume)
        {
            if (plume == null || !PlumeWaiting(plume)) yield break;
            // Bu diken biz patlatana kadar kendi kendine patlamasın.
            if (!_anticWaitOrig.ContainsKey(plume))
            {
                float ow = SetAnticWait(plume, 60f);
                if (ow >= 0f) _anticWaitOrig[plume] = ow;
            }
            PlayMakerFSM pf = plume.GetComponent<PlayMakerFSM>();
            tk2dSpriteAnimator pan = plume.GetComponent<tk2dSpriteAnimator>();
            Vector3 psz = ClipMaxSize(pan, "Plume Burst");
            float baseY = plume.transform.position.y + psz.z * Mathf.Abs(plume.transform.lossyScale.y);
            float gy; if (GroundUnderHero(out gy) && Mathf.Abs(gy - baseY) < 3f) baseY = gy;   // yerden çıksın

            string heroLayer = "Default"; int heroOrder = 0; float heroZ = 0.004f;
            if (HeroController.instance != null)
            {
                Renderer hr = HeroController.instance.GetComponent<Renderer>();
                if (hr != null) { heroLayer = hr.sortingLayerName; heroOrder = hr.sortingOrder; }
                heroZ = HeroController.instance.transform.position.z;
            }
            // Uyarı tam olarak tendril'in çıkacağı yerin ve genişliğinin üstünde olsun.
            float mx0 = plume.transform.position.x, mw = MarkerWidth;
            float tk, smin, smax; Vector3 tpos, tps, tts;
            if (TendrilPlacement(plume, PickFor(plume), out tk, out tpos, out smin, out smax, out tps, out tts))
            {
                mx0 = (smin + smax) / 2f;
                mw = Mathf.Clamp(smax - smin, MarkerMinWidth, MarkerWidth);
            }
            GameObject fx = new GameObject("Parry Wave2 Marker");
            fx.transform.position = new Vector3(mx0, baseY, heroZ + 0.003f);
            // Kara baloncuklar: aşağıdan yukarı hızla yükselip küçülerek kaybolur.
            ParticleSystem drops = MakeParticles(fx.transform, "Marker Drops", DropTex(), mw, -0.15f, 0.3f, MarkerBubbleRate / mw,
                new Vector2(0.4f, 0.7f), new Vector2(0.25f, 0.65f), new Color(0f, 0f, 0f, 1f), new Color(0.04f, 0.03f, 0.05f, 1f),
                new Vector2(3.5f, 7.5f), 0.15f, 0f, 0.2f, heroLayer, heroOrder);
            // Tabanda küçük koyu duman.
            ParticleSystem smoke = MakeParticles(fx.transform, "Marker Smoke", SmokeTex(), mw * 0.8f, -0.2f, 0.2f, 10f / mw,
                new Vector2(0.35f, 0.7f), new Vector2(0.8f, 1.6f), new Color(0.02f, 0.02f, 0.03f, 0.9f), new Color(0.08f, 0.06f, 0.1f, 0.7f),
                new Vector2(0.6f, 1.6f), 0.1f, 30f, 1.3f, heroLayer, heroOrder);
            drops.Play(true); smoke.Play(true);
            drops.Emit(10); smoke.Emit(3);   // boş başlamasın, hemen belirsin

            // Diken patlayana kadar (Antic bitene kadar) dikenin uyarı görüntüsünü gizli tut.
            float timeout = Time.time + 6f;
            while (plume != null && plume.activeInHierarchy && Time.time < timeout)
            {
                string st = pf != null ? pf.ActiveStateName : "";
                if (st != "Init" && st != "Outside Arena?" && st != "Shift Y" && st != "Antic") break;
                foreach (Renderer rd in plume.GetComponentsInChildren<Renderer>()) if (rd.enabled) rd.enabled = false;
                yield return null;
            }
            // Diken hiç patlatılmadıysa (ceza yarıda kaldıysa) bekleme süresini geri ver.
            float ow3;
            if (PlumeWaiting(plume) && _anticWaitOrig.TryGetValue(plume, out ow3))
            {
                SetAnticWait(plume, ow3); _anticWaitOrig.Remove(plume);
                // Kendi kendine patlarsa görünmez olmasın.
                foreach (Renderer rd in plume.GetComponentsInChildren<Renderer>(true)) rd.enabled = true;
            }
            // Baloncuklar dursun, havadakiler kaybolunca sil.
            if (drops != null) drops.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            yield return new WaitForSeconds(1f);
            if (fx != null) UnityEngine.Object.Destroy(fx);
        }




        // Daraltılmış diken hitbox'ının dünya birimindeki genişliği (dikenin en geniş hasar çokgenine göre).
        private static float SpikeHitboxWidth(GameObject plume)
        {
            float best = 0f;
            foreach (PolygonCollider2D c in plume.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                float mn = float.MaxValue, mx = float.MinValue;
                for (int p = 0; p < c.pathCount; p++) foreach (Vector2 q in c.GetPath(p)) { mn = Mathf.Min(mn, q.x); mx = Mathf.Max(mx, q.x); }
                if (mx > mn) best = Mathf.Max(best, (mx - mn) * Mathf.Abs(c.transform.lossyScale.x));
            }
            return best > 0f ? best * PlumeHitboxWidth : MarkerWidth;
        }

        private const float TendrilStraightStart = 0.15f;   // çıkışın ilk bu kadar saniyesi düz hitbox
        private const float TendrilStraightEnd = 0.15f;     // inişin son bu kadar saniyesi düz hitbox
        private float _plumeRetractDelay = -1f;            // diken çıktıktan kaç saniye sonra geri çekilmeye başlıyor (1. dalgadan ölçülür)

        private IEnumerator MeasurePlumeRetract(GameObject plume, float t0)
        {
            PlayMakerFSM pf = plume != null ? plume.GetComponent<PlayMakerFSM>() : null;
            if (pf == null) yield break;
            float timeout = Time.time + 4f;
            while (plume != null && plume.activeInHierarchy && Time.time < timeout)
            {
                string st = pf.ActiveStateName;
                if (st == "Plume 3" || st == "End")
                {
                    bool first = _plumeRetractDelay < 0f;
                    _plumeRetractDelay = Time.time - t0;
                    yield break;
                }
                yield return null;
            }
        }

        // Dümdüz yukarı kutu: tendril'in tabanında, o anki boyu kadar.
        private static void UpdateStraightBox(GameObject sb, BoxCollider2D sbox, LineRenderer line, Renderer trd, float baseX, float w)
        {
            if (sb == null || sbox == null || trd == null) return;
            Bounds b = trd.bounds;
            float h = b.size.y * TendrilBoxHeight;
            if (h < 0.05f) { sbox.enabled = false; if (line != null) line.positionCount = 0; return; }
            sb.transform.position = new Vector3(baseX, b.min.y + h / 2f, 0f);
            sbox.offset = Vector2.zero;
            sbox.size = new Vector2(w, h);
            sbox.enabled = true;
            if (line != null)
            {
                float x0 = baseX - w / 2f, x1 = baseX + w / 2f, y0 = b.min.y, y1 = b.min.y + h;
                line.positionCount = 5;
                line.SetPosition(0, new Vector3(x0, y0, -0.5f)); line.SetPosition(1, new Vector3(x1, y0, -0.5f));
                line.SetPosition(2, new Vector3(x1, y1, -0.5f)); line.SetPosition(3, new Vector3(x0, y1, -0.5f));
                line.SetPosition(4, new Vector3(x0, y0, -0.5f));
            }
        }

        // Görünmez dikenin kendi çarpışma alanlarını kapatır: tendril'e vurunca çınlama sesi ve geri sekme olmasın.
        // (Dikenin durum makinesi her adımda bunları yeniden açtığı için her karede kapatılır; hasarı kendi hitbox'larımız veriyor.)
        private static void MutePlumeColliders(GameObject plume)
        {
            if (plume == null) return;
            foreach (Collider2D c in plume.GetComponentsInChildren<Collider2D>()) if (c.enabled) c.enabled = false;
        }

        private IEnumerator TendrilOnPlume(GameObject plume, int index)
        {
            if (plume == null || _tendrilPrefabs.Count == 0) yield break;
            TendrilPick pick = PickFor(plume);
            _tendrilPicks.Remove(plume);   // havuzdan tekrar kullanılınca yeni seçim yapılsın
            int kind = pick.kind;
            GameObject prefab = _tendrilPrefabs[kind];
            string upClip = "Tendril" + (kind + 1) + " Up", downClip = "Tendril" + (kind + 1) + " Down";
            // TendrilPaths sırası Tendril1..5 olduğu için klip adı sırayla eşleşir.

            // Dikenin görünümünü gizle. İlk anlarda dikenin kendi hitbox'ı vurur, sonra tendril'in şekline uyan alan.
            var renderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer rd in plume.GetComponentsInChildren<Renderer>()) if (rd.enabled) { rd.enabled = false; renderers.Add(rd); }
            var dmg = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DamageHero, int>>();
            foreach (DamageHero dh in plume.GetComponentsInChildren<DamageHero>(true))
            {
                dmg.Add(new System.Collections.Generic.KeyValuePair<DamageHero, int>(dh, dh.damageDealt));
                dh.damageDealt = 0;   // dikenin kendi geniş hitbox'ı vurmaz; ilk anlarda onun daraltılmış kopyası vurur
            }
            int hitLayer = dmg.Count > 0 && dmg[0].Key != null ? dmg[0].Key.gameObject.layer : 11;
            // Dikenin çarpışma alanlarının şimdiki halini sakla: biz kapatıyoruz, sonunda (havuzda tekrar kullanılacağı için) geri yüklenir.
            var colSnap = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Collider2D, bool>>();
            foreach (Collider2D c in plume.GetComponentsInChildren<Collider2D>(true)) colSnap.Add(new System.Collections.Generic.KeyValuePair<Collider2D, bool>(c, c.enabled));

            // Tendril kopyası: kendi durum makineleri ve hasar/çarpışma parçaları silinir, sadece görüntü kalır.
            GameObject t = UnityEngine.Object.Instantiate(prefab);
            foreach (PlayMakerFSM f in t.GetComponentsInChildren<PlayMakerFSM>(true)) UnityEngine.Object.Destroy(f);
            foreach (DamageHero dh in t.GetComponentsInChildren<DamageHero>(true)) UnityEngine.Object.Destroy(dh);
            foreach (Collider2D c in t.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            t.SetActive(true);
            tk2dSpriteAnimator an = t.GetComponent<tk2dSpriteAnimator>();

            // Boyut ve konum: tendril'in en uzun karesi dikenin boyuna gelir, alt kenarları aynı yerde (yerden çıkar).
            float k, spanMin, spanMax; Vector3 tpos, ps, ts;
            TendrilPlacement(plume, pick, out k, out tpos, out spanMin, out spanMax, out ps, out ts);
            float flip = pick.flip;
            Vector3 plScale = plume.transform.lossyScale;
            float plumeH = ps.y * Mathf.Abs(plScale.y);
            t.transform.localScale = new Vector3(k * TendrilWidthMul * flip, k, 1f);
            t.transform.position = tpos;
            float spikeOffset = UseTendrilShapeHitbox ? (spanMin + spanMax) / 2f - plume.transform.position.x : 0f;   // daraltılmış diken hitbox'ı tendril'in tabanına kayar
            if (!_tendrilSizeLogged)
            {
                _tendrilSizeLogged = true;
            }

            // Hasar alanı: tendril'in her karesinde resmin şekline göre yeniden kurulur.
            GameObject hb = new GameObject("HK Plume Void Tendril Hitbox");
            hb.layer = hitLayer;
            hb.transform.SetParent(t.transform, false);
            Rigidbody2D hrb = hb.AddComponent<Rigidbody2D>(); hrb.isKinematic = true; hrb.gravityScale = 0f;
            PolygonCollider2D poly = hb.AddComponent<PolygonCollider2D>(); poly.isTrigger = true; poly.enabled = false;
            DamageHero hdh = hb.AddComponent<DamageHero>(); hdh.damageDealt = TendrilDamage; hdh.hazardType = 1;
            LineRenderer line = null;
            tk2dSprite tsp = t.GetComponent<tk2dSprite>();
            Renderer trd = t.GetComponent<Renderer>();
            int lastId = -1;
            _tendrilsAlive++;
            float tStart = Time.time;

            // Düz hitbox: çıkışın ilk ve inişin son anlarında tendril dümdüz yukarı bir kutu gibi vurur
            // (tendril'in eğri, geniş açılan kısmı bu anlarda saçma yerlerden vurmasın).
            float straightW = SpikeHitboxWidth(plume);
            float baseX = UseTendrilShapeHitbox ? (spanMin + spanMax) / 2f : plume.transform.position.x;
            GameObject sb = new GameObject("HK Plume Void Straight Hitbox");
            sb.layer = hitLayer;
            Rigidbody2D sbrb = sb.AddComponent<Rigidbody2D>(); sbrb.isKinematic = true; sbrb.gravityScale = 0f;
            BoxCollider2D sbox = sb.AddComponent<BoxCollider2D>(); sbox.isTrigger = true; sbox.enabled = false;
            DamageHero sbdh = sb.AddComponent<DamageHero>(); sbdh.damageDealt = TendrilDamage; sbdh.hazardType = 1;
            LineRenderer sbl = null;
            // Geri çekilme animasyonunun süresi.
            float downDur = 0.25f;
            try
            {
                tk2dSpriteAnimationClip dc = an != null && an.Library != null ? an.Library.GetClipByName(downClip) : null;
                if (dc != null && dc.frames != null && dc.fps > 0f) downDur = dc.frames.Length / dc.fps;
            }
            catch (System.Exception) { }
            bool wasStraight = false;
            bool inDown = false; float downStart = 0f;

            if (an != null) { try { an.Play(upClip); } catch (System.Exception) { } }

            // Diken geri çekilmeye başlayınca (Plume 3 / End) tendril de geri çekilsin.
            PlayMakerFSM pf = plume.GetComponent<PlayMakerFSM>();
            float timeout = Time.time + 3f;
            float tEnd = float.MaxValue, tMin = 0f;
            while (t != null)
            {
                if (!inDown)
                {
                    string st = pf != null && plume != null ? pf.ActiveStateName : "";
                    bool gone = plume == null || !plume.activeInHierarchy || Time.time >= timeout;
                    if (gone || st == "Plume 3" || st == "End" || st == "Deactivate" || st == "Recycle")
                    {
                        inDown = true; downStart = Time.time;
                        if (an != null) { try { an.Play(downClip); } catch (System.Exception) { } }
                        tEnd = Time.time + 1.0f; tMin = Time.time + 0.05f;
                    }
                }
                else if (Time.time >= tEnd || (Time.time >= tMin && (an == null || !an.Playing))) break;

                MutePlumeColliders(plume);
                // Hangi hitbox: düz mü, şekilli mi?
                float straightEnd;
                if (inDown) straightEnd = downStart + downDur - TendrilStraightEnd;
                else straightEnd = _plumeRetractDelay > 0f ? tStart + _plumeRetractDelay + downDur - TendrilStraightEnd : float.MaxValue;
                bool straight = Time.time < tStart + TendrilStraightStart || Time.time >= straightEnd;

                if (straight)
                {
                    if (poly != null) poly.enabled = false;
                    if (line != null) line.positionCount = 0;
                    UpdateStraightBox(sb, sbox, sbl, trd, baseX, straightW);
                }
                else
                {
                    sbox.enabled = false; if (sbl != null) sbl.positionCount = 0;
                    if (wasStraight) lastId = -1;
                    lastId = UpdateTendrilHitbox(tsp, poly, line, lastId);
                }
                wasStraight = straight;
                yield return null;
            }
            if (sb != null) UnityEngine.Object.Destroy(sb);
            GameObject spike = null;
            if (t != null) UnityEngine.Object.Destroy(t);
            if (spike != null) UnityEngine.Object.Destroy(spike);
            // Son tendril de yok olunca zemindeki void insin.
            _tendrilsAlive--;
            if (_tendrilsAlive <= 0) { _tendrilsAlive = 0; if (!_voidFloorHold) _voidFloorDown = true; }

            // Diken tamamen bitene kadar bekle, sonra hasarını eski haline getir (havuzdan tekrar kullanılacak).
            // Görünümüne dokunmuyoruz: dikenin kendi durum makinesi bir sonraki kullanımda kendisi açıyor.
            timeout = Time.time + 2f;
            while (plume != null && plume.activeInHierarchy && Time.time < timeout)
            {
                string st = pf != null ? pf.ActiveStateName : "";
                if (st == "End" || st == "Auto?" || st == "Deactivate" || st == "Recycle") break;
                MutePlumeColliders(plume);
                yield return null;
            }
            foreach (var kv in dmg) if (kv.Key != null) kv.Key.damageDealt = kv.Value;
            foreach (var kv in colSnap) if (kv.Key != null) kv.Key.enabled = kv.Value;
            float ow2;
            if (_anticWaitOrig.TryGetValue(plume, out ow2)) { SetAnticWait(plume, ow2); _anticWaitOrig.Remove(plume); }
        }


        private bool _holdBurst;
        // Pure Vessel Burst Pause'dayken beklemesini her karede baştan başlatır: süre dolup Plume Up'a geçemez.
        private IEnumerator HoldBurstPause(PlayMakerFSM m)
        {
            // Ceza sürerken Pure Vessel'in kendi Plume Up'ına giden bütün yolları geçici olarak Burst Pause'a
            // çeviriyoruz (bekleme bitse bile kendi hareketini yapamaz; ses/sarsıntı/kılıç çekme üst üste binmez).
            HutongGames.PlayMaker.FsmState burst = null;
            foreach (var st in m.FsmStates) if (st.Name == "Burst Pause") burst = st;
            var patched = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>>();
            if (burst != null)
                foreach (var st in m.FsmStates)
                    if (st.Transitions != null)
                        foreach (var tr in st.Transitions)
                            if (tr.ToState == "Plume Up")
                            {
                                patched.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>(tr, tr.ToFsmState));
                                tr.ToState = "Burst Pause";
                                tr.ToFsmState = burst;
                            }
            if (m.FsmGlobalTransitions != null)
                foreach (var tr in m.FsmGlobalTransitions)
                    if (tr.ToState == "Plume Up" && burst != null)
                    {
                        patched.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>(tr, tr.ToFsmState));
                        tr.ToState = "Burst Pause";
                        tr.ToFsmState = burst;
                    }

            float timeout = Time.time + 8f;
            string prev = m.ActiveStateName;
            while (_holdBurst && m != null && Time.time < timeout)
            {
                string cur = m.ActiveStateName;
                prev = cur;
                yield return null;
            }
            // Yolları geri aç.
            foreach (var kv in patched)
            {
                kv.Key.ToState = "Plume Up";
                kv.Key.ToFsmState = kv.Value;
            }
            _holdBurst = false;
        }

        private bool _voidFloorHold;   // void formunda: iki dalga arasında zemindeki void inmesin

        private static bool PvStunned(PlayMakerFSM m) { return m != null && System.Array.IndexOf(PvStunStates, m.ActiveStateName) >= 0; }

        // Pure Vessel ceza başlamadan stun yerse ceza iptal: her şey eski haline döner.
        private void AbortPunish(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> waits, HealthManager pvHm)
        {
            _holdBurst = false; _markersWatch = false;
            if (waits != null) foreach (var kv in waits) kv.Key.Value = kv.Value;
            _voidFloorHold = false; _voidFloorDown = true;
            if (pvHm != null && _noHitBoss == pvHm) _noHitBoss = null;
            _pvPunishRunning = false;
        }

        private IEnumerator PvPlumePunish(GameObject pv, bool voidMode = false)
        {
            if (_pvPunishRunning) yield break;
            _pvPunishRunning = true;
            PlayMakerFSM m = MainFsm(pv);
            tk2dSpriteAnimator anim = pv.GetComponent<tk2dSpriteAnimator>();
            HealthManager pvHm = pv != null ? pv.GetComponent<HealthManager>() : null;
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> waits = null;
            float timeout;

            if (voidMode)
            {
                // Oyunun kendi diken çıkışı (Plume Up) hiç olmasın: 1. dalgayı da biz void tendril olarak çıkaracağız.
                waits = ScaleWaits(m, new[] { "Burst Pause" }, 1f);
                foreach (var kv in waits) kv.Key.Value = 30f;
                _holdBurst = true;
                GameManager.instance.StartCoroutine(HoldBurstPause(m));
                // 1. dalganın uyarıları da kara baloncuk.
                _markersWatch = true;
                GameManager.instance.StartCoroutine(WatchWave2Markers(new System.Collections.Generic.HashSet<GameObject>()));
                timeout = Time.time + 6f;
                while (pv != null && m != null && m.ActiveStateName != "Burst Pause" && !PvStunned(m) && Time.time < timeout) yield return null;
                if (PvStunned(m)) { AbortPunish(waits, pvHm); yield break; }
                if (pv == null || m == null || m.ActiveStateName != "Burst Pause")
                {
                    _holdBurst = false; _markersWatch = false;
                    if (waits != null) foreach (var kv in waits) kv.Key.Value = kv.Value;
                    _pvPunishRunning = false; yield break;
                }
                // Zemine void yükselir, hemen ardından 1. dalga tendril'leri.
                var pre = ActivePlumes();
                var anticW1 = new System.Collections.Generic.List<GameObject>();
                foreach (GameObject g in pre) if (PlumeWaiting(g)) anticW1.Add(g);
                if (anticW1.Count == 0) {  _holdBurst = false; _markersWatch = false; foreach (var kv in waits) kv.Key.Value = kv.Value; m.SetState("Stomp Recover"); _pvPunishRunning = false; yield break; }
                if (UseVoidFloor && _abyssPitPrefab != null)
                {
                    Vector3 wp0 = anticW1[0].transform.position;
                    Vector3 wsz0 = ClipMaxSize(anticW1[0].GetComponent<tk2dSpriteAnimator>(), "Plume Burst");
                    float floorY0 = wp0.y + wsz0.z * Mathf.Abs(anticW1[0].transform.lossyScale.y);
                    _voidFloorUp = false; _voidFloorDown = false; _voidFloorHold = true;
                    GameManager.instance.StartCoroutine(VoidFloorRoutine(m, floorY0, wp0.z));
                    yield return null; yield return null;
                    _voidFloorUp = true;
                }
                if (pvHm != null) { _noHitBoss = pvHm;  }
                yield return new WaitForSeconds(VoidFloorMoveTime + 0.05f);
                if (pv == null || m == null) { _holdBurst = false; _pvPunishRunning = false; yield break; }
                _markersWatch = false;
                _tendrilsAlive = 0;
                int n1 = 0;
                foreach (GameObject g in anticW1)
                {
                    PlayMakerFSM pf1 = g != null ? g.GetComponent<PlayMakerFSM>() : null;
                    if (pf1 == null) continue;
                    pf1.SendEvent("PLUMES UP");
                    if (UseVoidTendrils && _tendrilPrefabs.Count > 0) { GameManager.instance.StartCoroutine(TendrilOnPlume(g, n1)); n1++; }
                }
                try { GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake"); } catch (System.Exception) { }
                if (!PlayVoidTendrilSound(m)) RunNativeActions(m, "Plume Up");
            }
            else
            {
                // 1. dalganın çıkış anını (Plume Up) bekle.
                timeout = Time.time + 6f;
                while (pv != null && m != null && m.ActiveStateName != "Plume Up" && !PvStunned(m) && Time.time < timeout) yield return null;
                if (PvStunned(m)) { AbortPunish(waits, pvHm); yield break; }
                if (pv == null || m == null || m.ActiveStateName != "Plume Up") {  _pvPunishRunning = false; yield break; }
            }
            float wave1Start = Time.time;
            string waveState = voidMode ? "Burst Pause" : "Plume Up";
            // Yere saplama pozu: 1. dalganın başından 2. dalganın sonuna kadar bu pozda kalır.
            string stabClip0 = GetStateClip(m, "Stomp Land");
            if (anim != null && stabClip0 != null) { try { anim.Play(stabClip0); } catch (System.Exception) { } }
            yield return null;
            if (anim != null && stabClip0 != null && !anim.IsPlaying(stabClip0)) { try { anim.Play(stabClip0); } catch (System.Exception) { } }
            var wave1 = ActivePlumes();
            if (wave1.Count < 2) {  _holdBurst = false; if (waits != null) foreach (var kv in waits) kv.Key.Value = kv.Value; m.SetState("Stomp Recover"); _pvPunishRunning = false; yield break; }
            GameManager.instance.StartCoroutine(MeasurePlumeRetract(wave1[0], wave1Start));
            if (!_plumeInfoLogged)
            {
                _plumeInfoLogged = true;
                PlayMakerFSM pf = wave1[0].GetComponent<PlayMakerFSM>();
                if (pf != null)
                    foreach (var st in pf.FsmStates)
                    {
                        var acts = new System.Collections.Generic.List<string>();
                        if (st.Actions != null) foreach (var a2 in st.Actions) if (a2 != null) acts.Add(a2.GetType().Name);
                        var trs = new System.Collections.Generic.List<string>();
                        if (st.Transitions != null) foreach (var tr in st.Transitions) trs.Add(tr.EventName + "->" + tr.ToState);
                    }
                var pu = new System.Collections.Generic.List<string>();
                foreach (var st in m.FsmStates)
                    if (st.Name == "Plume Up" && st.Actions != null) foreach (var a2 in st.Actions) if (a2 != null) pu.Add(a2.GetType().Name);
            }
            var wave1Set = new System.Collections.Generic.HashSet<GameObject>(wave1);

            // Sütun aralığı ve merkez (1. dalga ortadan iki yana doğru diziliyor).
            var xs = new System.Collections.Generic.List<float>();
            foreach (GameObject g in wave1) xs.Add(g.transform.position.x);
            xs.Sort();
            var gaps = new System.Collections.Generic.List<float>();
            for (int i = 0; i + 1 < xs.Count; i++) if (xs[i + 1] - xs[i] > 0.8f) gaps.Add(xs[i + 1] - xs[i]);
            gaps.Sort();
            float step = gaps.Count > 0 ? gaps[gaps.Count / 2] : 3.7f;
            float center = pv.transform.position.x;
            for (int i = 0; i + 1 < xs.Count; i++) if (xs[i + 1] - xs[i] < 0.8f) { center = xs[i]; break; } // iki dikenin üst üste olduğu yer = merkez

            var vPlumes = m.FsmVariables.FindFsmInt("Plumes");
            var vL = m.FsmVariables.FindFsmFloat("PlumeL X");
            var vR = m.FsmVariables.FindFsmFloat("PlumeR X");
            if (vPlumes == null || vL == null || vR == null) {  _holdBurst = false; if (waits != null) foreach (var kv in waits) kv.Key.Value = kv.Value; m.SetState("Stomp Recover"); _pvPunishRunning = false; yield break; }

            // Burst Pause beklemesini çok uzat: Pure Vessel'i orada yere saplanmış pozda tutacağız.
            if (waits == null)
            {
                waits = ScaleWaits(m, new[] { "Burst Pause" }, 1f);
                foreach (var kv in waits) kv.Key.Value = 30f;
            }
            string stabClip = GetStateClip(m, "Stomp Land");

            // 2. dalga işaretleri: 1. dalga sürerken, OYUNUN KENDİ diken dizme adımıyla (aynı görünüm),
            // sütunlar yarım aralık kaydırılmış (1. dalganın boşluklarına denk gelir).
            // Plume Up "PLUMES UP" olayını gecikmeli gönderiyor olabilir; o gecikme bitmeden 2. dalga
            // işaretlerini koyarsak ilk işaretler bu olayı alıp erken patlıyor (videoda ortadaki 6 diken).
            float upDelay = 0f;
            foreach (var st in m.FsmStates)
            {
                if (st.Name != "Plume Up" || st.Actions == null) continue;
                foreach (var act in st.Actions)
                {
                    if (act == null || act.GetType().Name != "SendEventByName") continue;
                    var fe = act.GetType().GetField("sendEvent");
                    var ev = fe != null ? fe.GetValue(act) as HutongGames.PlayMaker.FsmString : null;
                    if (ev == null || ev.Value != "PLUMES UP") continue;
                    var fd = act.GetType().GetField("delay");
                    var dv = fd != null ? fd.GetValue(act) as HutongGames.PlayMaker.FsmFloat : null;
                    if (dv != null) upDelay = Mathf.Max(upDelay, dv.Value);
                }
            }
            float tMark = wave1Start + Mathf.Max(PvWave2MarkDelay, upDelay + 0.15f);
            while (pv != null && Time.time < tMark && !PvStunned(m)) yield return null;
            if (PvStunned(m)) { AbortPunish(waits, pvHm); yield break; }
            if (pv == null || m == null) { foreach (var kv in waits) kv.Key.Value = kv.Value; _pvPunishRunning = false; yield break; }
            if (m.ActiveStateName != waveState) { _holdBurst = false; foreach (var kv in waits) kv.Key.Value = kv.Value;  _pvPunishRunning = false; yield break; }
            // Diken sayacının başlangıç değeri: oyun bunu Stomp Land'de ayarlıyor (her dikende geri sayıyor).
            int plumeStart = 10;
            foreach (var st in m.FsmStates)
            {
                if (st.Name != "Stomp Land" || st.Actions == null) continue;
                foreach (var act in st.Actions)
                {
                    if (act == null || act.GetType().Name != "SetIntValue") continue;
                    var fv = act.GetType().GetField("intVariable");
                    var fi = act.GetType().GetField("intValue");
                    var target = fv != null ? fv.GetValue(act) as HutongGames.PlayMaker.FsmInt : null;
                    var val = fi != null ? fi.GetValue(act) as HutongGames.PlayMaker.FsmInt : null;
                    if (target != null && target.Name == "Plumes" && val != null) plumeStart = val.Value;
                }
            }
            vPlumes.Value = plumeStart;
            vL.Value = center - step / 2f;
            vR.Value = center + step / 2f;
            m.SetState("Plume Gen");
            // 2. dalganın uyarı yerleri: diken ucu yerine yerden yükselen kara void baloncukları.
            _markersWatch = true;
            GameManager.instance.StartCoroutine(WatchWave2Markers(wave1Set));
            // Burst Pause'a girince orada tutulsun (oyunun kendi Plume Up'ı erken gelip dikenleri
            // ikinci kez patlatmasın, sesler üst üste binmesin).
            if (!voidMode)
            {
                _holdBurst = true;
                GameManager.instance.StartCoroutine(HoldBurstPause(m));
            }
            if (!voidMode && UseVoidFloor && _abyssPitPrefab != null)
            {
                Vector3 wp = wave1[0].transform.position;
                tk2dSpriteAnimator wan = wave1[0].GetComponent<tk2dSpriteAnimator>();
                Vector3 wsz = ClipMaxSize(wan, "Plume Burst");
                float floorY = wp.y + wsz.z * Mathf.Abs(wave1[0].transform.lossyScale.y);
                _voidFloorUp = false;
                _voidFloorDown = false;
                GameManager.instance.StartCoroutine(VoidFloorRoutine(m, floorY, wp.z));
            }

            // 1. dalga bitene kadar, sonra kaçış süresi. Pure Vessel bu sırada Burst Pause'da bekler.
            // (Hiç patlamamış, uyarı halinde bekleyen bir diken 1. dalgaya karışmışsa onu bekleme: yoksa
            //  2. dalganın işaretleri kendi zamanlayıcısıyla erken patlıyordu.)
            timeout = wave1Start + 1.6f;
            bool w1ok = false;
            while (pv != null && Time.time < timeout)
            {
                bool allDone = true;
                foreach (GameObject g in wave1) if (!PlumeDone(g) && !PlumeWaiting(g)) { allDone = false; break; }
                if (allDone) { w1ok = true; break; }
                yield return null;
            }
            if (!w1ok)
            {
                var ws = new System.Collections.Generic.List<string>();
                foreach (GameObject g in wave1) { PlayMakerFSM f1 = g != null ? g.GetComponent<PlayMakerFSM>() : null; ws.Add(f1 != null ? f1.ActiveStateName : "yok"); }
            }
            if (PvStunned(m)) { AbortPunish(waits, pvHm); yield break; }
            _voidFloorUp = true;   // void zemini tendril'lerden hemen önce hızla yükselsin
            // Void tendril saldırısı başlarken Pure Vessel hasar almaz (toparlanıp yeni hareketine geçene kadar).
            if (pvHm != null && !voidMode) { _noHitBoss = pvHm;  }
            float tGo = Time.time + PvWave2Gap;
            while (pv != null && !PvStunned(m) && (Time.time < tGo || (m != null && m.ActiveStateName != "Burst Pause" && Time.time < tGo + 2f)))
                yield return null;
            if (PvStunned(m)) { AbortPunish(waits, pvHm); yield break; }

            // 2. dalgayı patlat: aynı "PLUMES UP" olayı. Pure Vessel yere saplanmış pozda kalır.
            var wave2 = new System.Collections.Generic.List<GameObject>();
            // 2. dalga: uyarı halinde bekleyen bütün dikenler. 1. dalgada hiç patlamamış (ör. arena kenarında takılmış)
            // dikenler de buna dahil; yoksa uyarıları yerde kalıp sonra görünmeden kendi kendine patlıyorlardı.
            foreach (GameObject g in ActivePlumes()) if (!wave1Set.Contains(g) || PlumeWaiting(g)) wave2.Add(g);
            var st2 = new System.Collections.Generic.Dictionary<string, int>();
            foreach (GameObject g in wave2)
            {
                PlayMakerFSM pf = g.GetComponent<PlayMakerFSM>();
                string k = pf != null ? pf.ActiveStateName : "?";
                st2[k] = st2.ContainsKey(k) ? st2[k] + 1 : 1;
                if (pf != null) pf.SendEvent("PLUMES UP");
            }
            try { GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake"); } catch (System.Exception) { }
            _markersWatch = false;   // uyarı baloncukları durur
            _voidFloorHold = false;   // 2. dalga çıktı: tendril'ler bitince void insin
            if (UseVoidTendrils && _tendrilPrefabs.Count > 0)
            {
                if (!voidMode) _tendrilsAlive = 0;
                int n = 0;
                foreach (GameObject g in wave2) if (g != null) { GameManager.instance.StartCoroutine(TendrilOnPlume(g, n)); n++; }
            }
            // 2. dalga sesi: Pure Vessel'in void tendril saldırısının sesi (oyunun kendi ses eylemi).
            if (!PlayVoidTendrilSound(m)) RunNativeActions(m, "Plume Up");
            // Dikenin kendi çıkış sesi: dikenin "PLUMES UP" ile geçtiği adımdaki sesler (bir kez, ortadaki dikenden).
            if (wave2.Count > 0)
            {
                GameObject mid = wave2[wave2.Count / 2];
                PlayMakerFSM pf = mid != null ? mid.GetComponent<PlayMakerFSM>() : null;
                if (pf != null)
                    foreach (var st in pf.FsmStates)
                        if (st.Transitions != null)
                            foreach (var tr in st.Transitions)
                                if (tr.EventName == "PLUMES UP") PlayStateSounds(pf, tr.ToState, mid.transform.position);
            }
            var stTxt = new System.Collections.Generic.List<string>();
            foreach (var kv in st2) stTxt.Add(kv.Key + "x" + kv.Value);

            // 2. dalga bitene kadar yere saplanmış pozda kal, sonra normal toparlanma ve yeni saldırı seçimi.
            float t2 = Time.time;
            timeout = Time.time + 2.5f;
            yield return new WaitForSeconds(PvWave2MinHold); // dikenler en az bu kadar görünür kalır
            while (pv != null && Time.time < timeout)
            {
                bool allDone = true;
                foreach (GameObject g in wave2) if (!PlumeDone(g)) { allDone = false; break; }
                if (allDone) break;
                yield return null;
            }
            if (pv != null && m != null)
            {
                if (m.ActiveStateName == "Burst Pause" || m.ActiveStateName == "Plume Gen" || m.ActiveStateName == "Plume Pause")
                {
                    // Normaldeki gibi: önce kılıcı yerden çekme hareketi, sonra toparlanma.
                    // Toparlanma başlarken Pure Vessel tekrar vurulabilir olur.
                    if (pvHm != null && _noHitBoss == pvHm) { _noHitBoss = null;  }
                    string pullClip = GetStateClip(m, "Plume Up");
                    if (anim != null && pullClip != null)
                    {
                        try { anim.Play(pullClip); } catch (System.Exception) { }
                        float tEnd = Time.time + 1.5f;
                        yield return null;
                        while (pv != null && anim != null && anim.IsPlaying(pullClip) && Time.time < tEnd
                               && m.ActiveStateName == "Burst Pause") yield return null;
                    }
                    if (pv != null && m != null && m.ActiveStateName == "Burst Pause") m.SetState("Stomp Recover");
                }
            }
            if (pvHm != null && _noHitBoss == pvHm) _noHitBoss = null;   // her durumda ceza bitince vurulabilir
            _holdBurst = false;
            _voidFloorHold = false;
            foreach (var kv in waits) kv.Key.Value = kv.Value; // Burst Pause süresini geri yükle
            _pvPunishRunning = false;
        }

        // Bu boss'a gelen vuruşlar yok sayılır: zırh/sekme tepkisi yok, hasar yok, havaya vurmuş gibi.
        private HealthManager _noHitBoss;
        private void OnEnemyHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hit)
        {
            if (_noHitBoss != null && self == _noHitBoss) return;
            // Saf iğne + rüya iğnesi vuruşu: sonraki 5 sn iğne hasarı %15 fazla (büyüler hariç).
            if (Time.time < _dreamBuffUntil && hit.AttackType == AttackTypes.Nail && hit.DamageDealt > 0)
            {
                int d0 = hit.DamageDealt;
                hit.DamageDealt = Mathf.RoundToInt(d0 * DreamNailBuffMul);
            }
            // Parry ile sersemleyen boss, sersemleme bitene kadar 1.5 kat hasar alır.
            if (_stunBonusActive && self != null && self.gameObject == _stunBonusBoss && hit.DamageDealt > 0)
            {
                int d0 = hit.DamageDealt;
                hit.DamageDealt = Mathf.RoundToInt(d0 * StunDamageMul);
            }
            bool voidPv = _voidFormDone && self != null && self.gameObject == _pvWatched && self.GetComponent<VoidSkin>() != null;
            if (voidPv) _voidHitFrame = Time.frameCount;
            orig(self, hit);
            if (voidPv) { try { VoidHitBubbles(self, hit); } catch (System.Exception) { } }
        }


        // ---------- Havada parry sıçraması ----------
        // Normal savrulmalı bir saldırıyı havada parry'lersen savrulmazsın, pogo yapmış gibi yukarı sıçrarsın (tam pogo).
        // Saldırının özel savrulması (hafif/orta/çok/max) varsa o savrulma olur, sıçrama olmaz.
        private IEnumerator ParryAirBounce(HeroController hc, int tokenBefore, bool pushRight, bool heavy)
        {
            yield return new WaitForFixedUpdate();   // parry duraksaması bitsin, yerçekimi geri gelsin
            if (hc == null || hc.cState.dead) yield break;
            if (_recoilToken != tokenBefore) {  yield break; }
            if (hc.cState.onGround)
            {
                // Bu arada yere indiyse normal savrulma.
                try { if (heavy) { if (pushRight) hc.RecoilRightLong(); else hc.RecoilLeftLong(); } else if (!PlayerData.instance.GetBool("equippedCharm_14")) { if (pushRight) hc.RecoilRight(); else hc.RecoilLeft(); } } catch { }
                yield break;
            }
            Rigidbody2D rb = hc.GetComponent<Rigidbody2D>();
            float y0 = hc.transform.position.y;
            bool native = false;
            try
            {
                // Oyunun kendi pogo sıçraması (iğneyle aşağı vurup sekme).
                var mi = typeof(HeroController).GetMethod("Bounce", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                if (mi != null) { mi.Invoke(hc, null); native = hc.cState.bouncing; }
            }
            catch (System.Exception) {  }
            if (!native && rb != null)
            {
                // Yedek: pogo yüksekliğini oyunun ayarlarından hesaplayıp o hızla fırlat.
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                float bv = 12f, bt = 0.1f;
                try
                {
                    var fv = typeof(HeroController).GetField("BOUNCE_VELOCITY", flags); if (fv != null) bv = System.Convert.ToSingle(fv.GetValue(hc));
                    var ft = typeof(HeroController).GetField("BOUNCE_TIME", flags); if (ft != null) bt = System.Convert.ToSingle(ft.GetValue(hc));
                }
                catch { }
                float g = Mathf.Abs(Physics2D.gravity.y) * (rb.gravityScale > 0.05f ? rb.gravityScale : 0.79f);
                float h = bv * bt + bv * bv / (2f * g);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Sqrt(2f * g * h));
            }

            float tEnd = Time.time + 1.5f, top = y0;
            yield return null;
            while (hc != null && rb != null && Time.time < tEnd && !hc.cState.onGround)
            {
                top = Mathf.Max(top, hc.transform.position.y);
                if (rb.velocity.y < -0.5f) break;
                yield return null;
            }
        }

        private static readonly string[] MantisLightRules =
        {
            "mantis lord|Dash Antic", "mantis lord|Dash Arrive", "mantis lord|Dash Attack", "mantis lord|Dash Recover"
        };
        private static readonly string[] MantisMediumRules =
        {
            "mantis lord|Dstab Arrive", "mantis lord|Dstab Attack", "mantis lord|Dstab Land"
        };


        // Bıçak "Fly" adımında: başta X Velocity hızıyla gider, her karede X Force kuvveti onu yavaşlatıp geri döndürür (bumerang).
        // Parry'de: o anki gidiş yönünün tersine sabit hızla düz gider (kuvvet 0 = yolda falso almaz).
        // Parry anında Knight'a değen bütün bıçaklar geri döner (2. fazda iki yandan gelen iki bıçak).
        // ---------- Traitor Lord: geri gönderilen dalga ve orak ----------
        // Yerden atıldığı dash: parry'lenince durur, biraz geri çekilir, toparlanıp yeni saldırısını seçer.
        private static readonly string[] TraitorDashRules =
        {
            "traitor lord|Attack Antic", "traitor lord|Feint", "traitor lord|Speed Check", "traitor lord|Set Speed",
            "traitor lord|Attack 1", "traitor lord|Attack Swipe"
        };
        private const float TraitorBackSpeed = 9f;    // geri çekilme hızı
        private const float TraitorBackTime = 0.2f;   // geri çekilme süresi (çok az)

        private IEnumerator TraitorDashCancel(GameObject tl, string from)
        {
            if (SkipCancelForStun(tl, "Traitor Lord geri çekilme")) yield break;
            PlayMakerFSM m = MainFsm(tl);
            Rigidbody2D rb = tl.GetComponent<Rigidbody2D>();
            if (m == null || rb == null) yield break;
            float dir = HeroController.instance != null && HeroController.instance.transform.position.x > tl.transform.position.x ? -1f : 1f;
            PauseFsm(m, true);
            float t0 = Time.time;
            while (tl != null && rb != null && Time.time - t0 < TraitorBackTime)
            {
                float k = 1f - (Time.time - t0) / TraitorBackTime;
                rb.velocity = new Vector2(dir * TraitorBackSpeed * k, rb.velocity.y);
                yield return null;
            }
            if (tl == null || m == null) yield break;
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            PauseFsm(m, false);
            // Saldırının kendi toparlanma adımı: saldırı kutusunu kapatır, toparlanır, sonra kendiliğinden yeni saldırı seçer.
            if (HasState(m, "Attack Recover")) m.SetState("Attack Recover");
        }

        private const int TraitorWaveDamage = 60;     // geri dönen uzun dalga Traitor Lord'a değerse
        private const int TraitorSickleDamage = 15;   // geri gönderilen orak Traitor Lord'a değerse
        private const float TraitorSickleSpeedMul = 1.5f; // geri giden orak geldiğinden bu kadar hızlı
        private readonly System.Collections.Generic.HashSet<GameObject> _returned = new System.Collections.Generic.HashSet<GameObject>();

        private static HealthManager FindTraitorLord()
        {
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
                if (hm != null && hm.gameObject.name.Contains("Traitor Lord") && hm.hp > 0) return hm;
            return null;
        }

        // Geri gönderilen şey artık Knight'a vurmasın. Oyun bu nesneleri tekrar kullanıyor (havuz): kaybolunca
        // ReturnedHitsBoss her şeyi eski haline getirir (sonraki dalga/orak yine normal çalışsın).
        private class ReturnedState
        {
            public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DamageHero, int>> dmg = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DamageHero, int>>();
            public System.Collections.Generic.List<PlayMakerFSM> fsms = new System.Collections.Generic.List<PlayMakerFSM>();
            public Vector3 scale;
        }
        private static ReturnedState DisableHeroDamage(GameObject o)
        {
            var st = new ReturnedState { scale = o.transform.localScale };
            foreach (DamageHero dh in o.GetComponentsInChildren<DamageHero>(true))
            {
                st.dmg.Add(new System.Collections.Generic.KeyValuePair<DamageHero, int>(dh, dh.damageDealt));
                dh.damageDealt = 0; dh.enabled = false;
            }
            return st;
        }
        private static void RestoreReturned(GameObject o, ReturnedState st)
        {
            if (o == null || st == null) return;
            foreach (var kv in st.dmg) if (kv.Key != null) { kv.Key.damageDealt = kv.Value; kv.Key.enabled = true; }
            foreach (PlayMakerFSM f in st.fsms) if (f != null) f.enabled = true;
            o.transform.localScale = st.scale;
        }

        private void TraitorWaveReturn(GameObject wave)
        {
            if (!_returned.Add(wave)) return;
            try
            {
                HealthManager tl = FindTraitorLord();
                Rigidbody2D rb = wave.GetComponent<Rigidbody2D>();
                if (rb == null) rb = wave.GetComponentInChildren<Rigidbody2D>();
                float vx0 = rb != null ? rb.velocity.x : 0f;
                float speed = Mathf.Max(Mathf.Abs(vx0), 10f);
                float dir = tl != null ? Mathf.Sign(tl.transform.position.x - wave.transform.position.x) : -Mathf.Sign(vx0 == 0f ? 1f : vx0);
                ReturnedState rs = DisableHeroDamage(wave);
                if (rb != null) rb.velocity = new Vector2(dir * speed, rb.velocity.y);
                if (vx0 != 0f && Mathf.Sign(vx0) != dir)
                {
                    Vector3 ls = wave.transform.localScale;
                    wave.transform.localScale = new Vector3(-ls.x, ls.y, ls.z);
                }
                GameManager.instance.StartCoroutine(ReturnedHitsBoss(wave, tl, TraitorWaveDamage, dir, "dalga", Vector3.zero, false, rs));
            }
            catch (System.Exception e) { Log("HATA (Traitor dalgası): " + e.Message); }
        }

        private void TraitorSickleReturn(GameObject sickle, HeroController hero)
        {
            if (!_returned.Add(sickle)) return;
            try
            {
                HealthManager tl = FindTraitorLord();
                float dir = hero != null && hero.cState.facingRight ? 1f : -1f;
                // Orak yatayda fiziksel hızla gider, dikeyde kendi iTween'iyle dalgalanır (log'daki teşhise göre).
                // Kendi hareketi aynen kalır; sadece yatay hızı baktığın yöne çevrilir.
                ReturnedState rs = DisableHeroDamage(sickle);
                Rigidbody2D rb = sickle.GetComponent<Rigidbody2D>();
                float vx0 = rb != null ? rb.velocity.x : 0f;
                float speed = Mathf.Max(Mathf.Abs(vx0), 8f) * TraitorSickleSpeedMul;
                if (rb != null) rb.velocity = new Vector2(dir * speed, rb.velocity.y);
                if (vx0 != 0f && Mathf.Sign(vx0) != dir)
                {
                    Vector3 ls = sickle.transform.localScale;
                    sickle.transform.localScale = new Vector3(-ls.x, ls.y, ls.z);
                }
                GameManager.instance.StartCoroutine(ReturnedHitsBoss(sickle, tl, TraitorSickleDamage, dir, "orak", Vector3.zero, true, rs));
            }
            catch (System.Exception e) { Log("HATA (Traitor orağı): " + e.Message); }
        }

        // Geri gönderilen şey bossa değerse bir kez hasar verir. manualVel != 0: biz hareket ettiririz; vanishOnHit: değince kaybolur.
        private IEnumerator ReturnedHitsBoss(GameObject proj, HealthManager boss, int dmg, float dir, string what, Vector3 manualVel, bool vanishOnHit, ReturnedState rs)
        {
            float tEnd = Time.time + 4f;
            bool hit = false;
            while (proj != null && proj.activeInHierarchy && Time.time < tEnd)
            {
                if (manualVel != Vector3.zero) proj.transform.position += manualVel * Time.deltaTime;
                if (!hit && boss != null && boss.hp > 0)
                {
                    bool touch = false;
                    foreach (Collider2D pc in proj.GetComponentsInChildren<Collider2D>())
                    {
                        if (pc == null || !pc.enabled) continue;
                        // Sadece bossun kendi gövdesi (alt nesnelerde büyük menzil algılayıcıları var, onlar sayılmaz).
                        foreach (Collider2D bc in boss.GetComponents<Collider2D>())
                            if (bc != null && bc.enabled && !bc.isTrigger && bc.bounds.Intersects(pc.bounds)) { touch = true; break; }
                        if (touch) break;
                    }
                    if (!touch)
                    {
                        // Uzun dalga gibi dikey şeyler için: yatayda bossun gövdesinin içine girdiyse de değmiş say.
                        Bounds? bb = null;
                        foreach (Collider2D bc in boss.GetComponents<Collider2D>())
                            if (bc != null && bc.enabled && !bc.isTrigger) { if (bb == null) bb = bc.bounds; else { Bounds t = bb.Value; t.Encapsulate(bc.bounds); bb = t; } }
                        if (bb != null)
                        {
                            float px = proj.transform.position.x;
                            Bounds pb = new Bounds(proj.transform.position, Vector3.zero);
                            foreach (Collider2D pc in proj.GetComponentsInChildren<Collider2D>(true)) if (pc != null) pb.Encapsulate(pc.bounds);
                            bool xIn = px >= bb.Value.min.x - 0.3f && px <= bb.Value.max.x + 0.3f;
                            bool yOverlap = pb.max.y >= bb.Value.min.y && pb.min.y <= bb.Value.max.y;
                            touch = xIn && yOverlap;
                        }
                    }
                    if (touch)
                    {
                        hit = true;
                        try
                        {
                            boss.Hit(new HitInstance
                            {
                                Source = proj, AttackType = AttackTypes.Generic, DamageDealt = dmg,
                                Direction = dir > 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true
                            });
                        }
                        catch (System.Exception e) { Log("HATA (" + what + " hasarı): " + e.Message); }
                        if (vanishOnHit)
                        {
                            // Kendi bitiş adımıyla kaybolsun (bitiş animasyonu oynar, sonra havuza döner).
                            PlayMakerFSM mv = null;
                            foreach (PlayMakerFSM f in proj.GetComponents<PlayMakerFSM>()) if (f.FsmName == "Movement") mv = f;
                            if (mv != null && HasState(mv, "End")) mv.SetState("End"); else proj.SetActive(false);
                            break;
                        }
                    }
                }
                yield return null;
            }
            if (proj != null && manualVel != Vector3.zero && proj.activeInHierarchy) proj.SetActive(false);   // ekrandan çıkınca kaybolsun
            // Dalga hâlâ duruyorsa kaybolmasını bekle (zararsız kalsın), sonra her şeyi eski haline getir.
            float tWait = Time.time + 8f;
            while (proj != null && proj.activeInHierarchy && Time.time < tWait) yield return null;
            if (proj != null && proj.activeInHierarchy) proj.SetActive(false);
            RestoreReturned(proj, rs);
            if (proj != null) _returned.Remove(proj);
        }

        private void MantisShotReturn(GameObject shot)
        {
            ReverseMantisShot(shot);
            try
            {
                Collider2D hc = HeroController.instance != null ? HeroController.instance.GetComponent<Collider2D>() : null;
                if (hc == null) return;
                Bounds hb = hc.bounds; hb.Expand(MantisShotParryReach);
                foreach (DamageHero dh in UnityEngine.Object.FindObjectsOfType<DamageHero>())
                {
                    GameObject o = dh.gameObject;
                    if (o == shot || !o.activeInHierarchy || !o.name.StartsWith("Shot Mantis Lord")) continue;
                    Collider2D c = o.GetComponent<Collider2D>();
                    if (c == null || !c.enabled || !c.bounds.Intersects(hb)) continue;
                    ReverseMantisShot(o);
                }
            }
            catch (System.Exception e) { Log("HATA (ikinci mantis bıçağı): " + e.Message); }
        }

        private const float MantisShotParryReach = 1.2f;   // ikinci bıçağın da parry'lenmesi için Knight'ın etrafındaki pay
        private const float MantisShotWait = 0.6f;          // bıçağı parry'lenen mantis bu kadar yerinde kalır, sonra hemen saldırır
        private const int MantisShotDamage = 20;            // geri dönen bıçağın mantise hasarı     // geri dönen bıçağın mantise hasarı: iğne hasarının %80'i

        private void ReverseMantisShot(GameObject shot)
        {
            try
            {
                Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
                PlayMakerFSM f = null;
                foreach (PlayMakerFSM x in shot.GetComponents<PlayMakerFSM>()) if (x.FsmName == "Control") f = x;
                if (rb == null || f == null) {  return; }
                var xv = f.FsmVariables.FindFsmFloat("X Velocity");
                var xf = f.FsmVariables.FindFsmFloat("X Force");
                float speed = xv != null && Mathf.Abs(xv.Value) > 1f ? Mathf.Abs(xv.Value) : 33f;
                float vx0 = rb.velocity.x;
                float dir = Mathf.Abs(vx0) > 0.5f ? -Mathf.Sign(vx0)
                          : (HeroController.instance != null && HeroController.instance.transform.position.x > shot.transform.position.x ? -1f : 1f);
                if (xf != null) xf.Value = 0f;
                if (xv != null) xv.Value = dir * speed;
                rb.velocity = new Vector2(dir * speed, 0f);
                GameManager.instance.StartCoroutine(MantisShotHits(shot, dir));
                // Atan mantis: bıçağı kendi "Projectile" değişkeninde tutan.
                foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
                {
                    if (!hm.gameObject.name.StartsWith("Mantis Lord")) continue;
                    PlayMakerFSM mm = MainFsm(hm.gameObject);
                    var pj = mm != null ? mm.FsmVariables.FindFsmGameObject("Projectile") : null;
                    if (pj != null && pj.Value == shot) { GameManager.instance.StartCoroutine(MantisNextAttack(mm)); return; }
                }
            }
            catch (System.Exception e) { Log("HATA (mantis bıçağı): " + e.Message); }
        }

        private static readonly string[] MantisWallWaitStates = { "Throw CD", "Throw CD 2", "Leave Pause", "Arrive Pause", "Keep Throwing?", "Keep Throwing? 2" };

        // Atan mantis duvardaki beklemeleri atlayıp hemen sonraki saldırısına geçer.
        // Geri dönen bıçak değdiği mantislere hasar verir (her mantise bir kez).
        private IEnumerator MantisShotHits(GameObject shot, float dir)
        {
            int nail = 5;
            try { nail = PlayerData.instance.GetInt("nailDamage"); } catch { }
            int dmg = MantisShotDamage;   // sabit 20 (eskiden iğne hasarının %80i)
            Collider2D sc = shot.GetComponent<Collider2D>();
            var done = new System.Collections.Generic.HashSet<HealthManager>();
            var mantises = new System.Collections.Generic.List<HealthManager>();
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
                if (hm.gameObject.name.StartsWith("Mantis Lord")) mantises.Add(hm);
            float tEnd = Time.time + 3f;
            yield return null;
            while (shot != null && shot.activeInHierarchy && sc != null && Time.time < tEnd)
            {
                Bounds sb = sc.bounds;
                foreach (HealthManager hm in mantises)
                {
                    if (hm == null || done.Contains(hm) || hm.hp <= 0) continue;
                    bool touch = false;
                    foreach (Collider2D c in hm.GetComponents<Collider2D>())
                        if (c.enabled && c.bounds.Intersects(sb)) { touch = true; break; }
                    if (!touch) continue;
                    done.Add(hm);
                    try
                    {
                        hm.Hit(new HitInstance
                        {
                            Source = shot, AttackType = AttackTypes.Generic, DamageDealt = dmg,
                            Direction = dir > 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true
                        });
                    }
                    catch (System.Exception e) { Log("HATA (bıçak hasarı): " + e.Message); }
                }
                yield return null;
            }
        }

        private IEnumerator MantisNextAttack(PlayMakerFSM m)
        {
            string from = m.ActiveStateName;
            // Önce bir süre yerinde kalsın (bıçağın geri dönüşünü görsün / bıçak ona değebilsin).
            float tw = Time.time + MantisShotWait;
            while (m != null && Time.time < tw) yield return null;
            if (m == null) yield break;
            var sub = m.FsmVariables.FindFsmBool("Sub");
            bool isSub = sub != null && sub.Value;
            float tEnd = Time.time + 3f;
            bool left = false;
            while (m != null && Time.time < tEnd)
            {
                string st = m.ActiveStateName;
                if (!left && System.Array.IndexOf(MantisWallWaitStates, st) >= 0 && HasState(m, "Wall Leave 1"))
                {
                    m.SetState("Wall Leave 1");   // duvardan ayrılma animasyonu (bekleme atlanır)
                    left = true;
                }
                else if (st == "After Throw Pause" || st == "Idle")
                {
                    if (!isSub && HasState(m, "Attack Choice"))
                    {
                        m.SetState("Attack Choice");
                    }
                    else if (isSub && HasState(m, "Sub Idle"))
                    {
                        // Sisters of Battle: kardeşlere saldırıyı savaş yöneticisi verir; burada atan kardeşe biz veriyoruz.
                        m.SetState("Sub Idle");
                        string ev = UnityEngine.Random.value < 0.5f ? "DASH" : "DSTAB";
                        m.SendEvent(ev);
                    }
                    yield break;
                }
                else if (st != "Wall Leave 1" && st != "Wall Leave 2" && System.Array.IndexOf(MantisWallWaitStates, st) < 0
                         && !st.StartsWith("Throw"))
                {
                    yield break;
                }
                yield return null;
            }
        }

        // Mantis yandan dash'i: arenanın öbür ucuna kadar değil, senin olduğun yere kadar gider (Sly'daki gibi).
        private readonly System.Collections.Generic.HashSet<PlayMakerFSM> _mantisDashStopped = new System.Collections.Generic.HashSet<PlayMakerFSM>();
        private void MantisDashTick(HeroController hero)
        {
            if (hero == null) return;
            foreach (PlayMakerFSM c in _bossControls)
            {
                if (c == null || !c.enabled || !c.gameObject.name.StartsWith("Mantis Lord")) continue;
                if (c.ActiveStateName != "Dash Attack") { _mantisDashStopped.Remove(c); continue; }
                if (_mantisDashStopped.Contains(c)) continue;
                Rigidbody2D rb = c.GetComponent<Rigidbody2D>();
                float vx = rb != null ? rb.velocity.x : 0f;
                if (Mathf.Abs(vx) < 1f) continue;
                float bx = c.transform.position.x, hx = hero.transform.position.x;
                bool reached = vx < 0f ? bx <= hx : bx >= hx;
                if (!reached) continue;
                _mantisDashStopped.Add(c);
                if (HasState(c, "Dash Recover")) c.SetState("Dash Recover");   // durur, toparlanıp ayrılır
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        // ---------- Hornet ----------
        private static readonly string[] HornetDashRules =
        {
            "hornet boss|ADash Antic", "hornet boss|A Dash", "hornet boss|GDash Antic", "hornet boss|G Dash"
        };
        private const float HornetBackSpeed = 18f;   // geriye kaçma hızı
        private const float HornetBackTime = 0.25f;  // geriye kaçma süresi

        private static GameObject FindHornet()
        {
            GameObject h = GameObject.Find("Hornet Boss 1");
            if (h == null) h = GameObject.Find("Hornet Boss 2");
            return h;
        }

        private IEnumerator HornetDashCancel(GameObject hornet, string from)
        {
            PlayMakerFSM m = MainFsm(hornet);
            Rigidbody2D rb = hornet.GetComponent<Rigidbody2D>();
            if (m == null) yield break;
            if (SkipCancelForStun(hornet, "Hornet geri kaçma"))
            {
                // Sersemleme kazanır. Ama hava dash'indeysek oyunun dash ayarlarını geri al (yoksa zeminden düşer).
                if (from == "A Dash" || from == "ADash Antic") RestoreHornetAfterAirDash(m);
                foreach (string hn in new[] { "Hit ADash", "Hit GDash" })
                {
                    Transform t = hornet.transform.Find(hn);
                    if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
                }
                yield break;
            }
            HeroController h = HeroController.instance;
            float dir = h != null && h.transform.position.x > hornet.transform.position.x ? -1f : 1f;   // Knight'tan uzağa
            // Dash'in vuruş alanları kapansın: yoksa kaçarken de vurur (bir sonraki dash'te oyun yine açar).
            foreach (string hn in new[] { "Hit ADash", "Hit GDash" })
            {
                Transform t = hornet.transform.Find(hn);
                if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
            }
            bool air = from == "A Dash" || from == "ADash Antic";
            if (!air)
            {
                // Yerde: oyunun kendi geri kaçma hareketi (Evade).
                if (HasState(m, "Evade")) m.SetState("Evade");
            }
            else
            {
                // Havada Evade kullanılamaz (yere inene kadar uçup ekrandan çıkıyordu): geriye ve biraz yukarı seker,
                // sonra normal düşüşüne geçer ("In Air": yere inince kendisi toparlanır).
                // Dash başlarken ("Fire") oyun Hornet'in çarpışma kutusunu zemine çarpmayan hale getirip döndürüyor;
                // normalde yere/tavana/duvara değince geri alıyor. Biz dash'i kestiğimiz için bu geri alınmıyordu
                // ve Hornet zeminden geçip haritanın altına düşüyordu. Oyunun tavana çarpma adımındaki ("Hit Roof")
                // geri alma işlemlerini (ışınlama ve durdurma hariç) burada çalıştırıyoruz.
                RestoreHornetAfterAirDash(m);
                if (HasState(m, "In Air")) m.SetState("In Air");
            }
            float t0 = Time.time;
            float T = air ? 0.3f : HornetBackTime;
            while (hornet != null && rb != null && Time.time - t0 < T)
            {
                float k = 1f - 0.6f * (Time.time - t0) / T;
                float vy = air ? (Time.time - t0 < 0.08f ? 6f : rb.velocity.y) : Mathf.Min(rb.velocity.y, 0f);
                rb.velocity = new Vector2(dir * (air ? 12f : HornetBackSpeed) * k, vy);
                yield return null;
            }
            if (air && rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }


        private void RestoreHornetAfterAirDash(PlayMakerFSM m)
        {
            int ran = 0;
            foreach (var st in m.FsmStates)
            {
                if (st.Name != "Hit Roof" || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null) continue;
                    string n = a.GetType().Name;
                    if (n == "SetPosition" || n == "SetVelocity2d") continue;   // tavana ışınlanmasın, savrulması bozulmasın
                    try { a.OnEnter(); ran++; }
                    catch (System.Exception) {  }
                }
            }
            // Yerçekimi oyunun kendi değeriyle (yere inince de bu kullanılıyor).
            Rigidbody2D rb = m.GetComponent<Rigidbody2D>();
            var g = m.FsmVariables.FindFsmFloat("Gravity");
            if (rb != null) rb.gravityScale = g != null && g.Value > 0.1f ? g.Value : 2.5f;
            Collider2D c = m.GetComponent<Collider2D>();
            if (c != null) c.isTrigger = false;
            m.transform.rotation = Quaternion.identity;
        }


        private IEnumerator HornetNeedleReturn(GameObject hornet, GameObject needle)
        {
            PlayMakerFSM m = MainFsm(hornet);
            string hs = m != null ? m.ActiveStateName : "?";
            if (hs != "Throw" && hs != "Thrown" && hs != "Throw Antic") {  yield break; }
            // İğnenin kendi durum makinesinde geri dönüş durumunu bul.
            bool done = false;
            foreach (PlayMakerFSM f in needle.GetComponents<PlayMakerFSM>())
            {
                string names = "";
                string ret = null;
                foreach (var st in f.FsmStates)
                {
                    names += st.Name + ", ";
                    string l = st.Name.ToLowerInvariant();
                    if (ret == null && (l.Contains("return") || l.Contains("back"))) ret = st.Name;
                }
                if (ret != null && f.ActiveStateName != ret) { f.SetState(ret); done = true;  }
            }
            // İğne bulunduğu yerden Hornet'e hızla geri uçsun (iğnenin kendi dönüşü olmasa da).
            if (!done)
            {
                Rigidbody2D nrb = needle.GetComponent<Rigidbody2D>();
                float t0 = Time.time;
                while (needle != null && hornet != null && Time.time - t0 < 0.4f)
                {
                    Vector3 to = hornet.transform.position - needle.transform.position;
                    if (to.magnitude < 0.8f) break;
                    Vector3 step = to.normalized * Mathf.Min(to.magnitude, 45f * Time.deltaTime);
                    if (nrb != null) nrb.velocity = Vector2.zero;
                    needle.transform.position += step;
                    yield return null;
                }
            }
            // Hornet'i hemen "Throw Recover"a geçirmek iğneyi olduğu yerden eline ışınlıyordu.
            // Artık iğne kendi "Return" hareketiyle geri uçar, Hornet'e varınca oyun kendisi haber verir (Notify).
            // Sadece bir şey takılırsa 1.5 sn sonra Hornet toparlanmaya zorlanır.
            float tEnd = Time.time + 1.5f;
            while (m != null && Time.time < tEnd && (m.ActiveStateName == "Thrown" || m.ActiveStateName == "Throw"))
                yield return null;
            if (m != null && HasState(m, "Throw Recover") && (m.ActiveStateName == "Thrown" || m.ActiveStateName == "Throw"))
            {
                m.SetState("Throw Recover");
            }
            else if (m != null) { }
        }

        // ---------- Massive Moss Charger ----------
        private static readonly string[] MossChargeRules = { "mega moss charger|Charge", "mega moss charger|Emerge" };

        // Durum makinesini kısa süre durdurup kaldığı yerden devam ettirmek için.
        // (Normalde tekrar açılınca BAŞTAN başlıyor: Moss Charger donup kalıyor, Fluke yaratıkları kayboluyordu.)
        private static void PauseFsm(PlayMakerFSM f, bool pause)
        {
            if (f == null) return;
            if (pause) { f.Fsm.RestartOnEnable = false; f.enabled = false; }
            else f.enabled = true;
        }

        private IEnumerator MossChargerPush(GameObject moss, HeroController hero)
        {
            Rigidbody2D rb = moss.GetComponent<Rigidbody2D>();
            if (rb == null) yield break;
            // Vurulunca normalde ne kadar savrulduğunu oyunun Recoil ayarından oku, 2 katını uygula.
            // Moss Charger'da bu ayar 0 (vurulunca hiç savrulmuyor): o zaman oyunun normal düşman savrulması kullanılır.
            float speed = 0f, dur = 0f;
            Recoil rc = moss.GetComponent<Recoil>();
            if (rc != null)
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                try
                {
                    var fs = typeof(Recoil).GetField("recoilSpeedBase", flags); if (fs != null) speed = System.Convert.ToSingle(fs.GetValue(rc));
                    var fd = typeof(Recoil).GetField("recoilDuration", flags); if (fd != null) dur = System.Convert.ToSingle(fd.GetValue(rc));
                }
                catch { }
            }
            if (speed * dur < 0.1f) { speed = 15f; dur = 0.15f; }
            float dist = 2f * speed * dur;
            float time = Mathf.Max(0.25f, 2f * dur);
            float v = dist / time;
            float dir = hero != null && hero.transform.position.x > moss.transform.position.x ? -1f : 1f;
            Vector2 v0 = rb.velocity;   // hücum hızı: savrulmadan sonra hücumuna devam etsin
            PlayMakerFSM m = MainFsm(moss);
            PauseFsm(m, true);
            float t0 = Time.time;
            while (moss != null && rb != null && Time.time - t0 < time)
            {
                rb.velocity = new Vector2(dir * v, rb.velocity.y);
                yield return null;
            }
            PauseFsm(m, false);
            if (rb != null) rb.velocity = new Vector2(Mathf.Abs(v0.x) > 1f ? v0.x : 0f, rb.velocity.y);
        }

        // =====================================================
        //  NORMAL DÜŞMANLAR (EnemyScout haritalarından)
        // =====================================================
        // Uyarı verilen normal düşmanlar (durum makineleri izlenir) ve bu mesafeden uzaktalarsa uyarı yok.
        private static readonly string[] NormalWarnEnemies = { "zombie guard", "plant trap", "mossman_shaker", "bursting zombie",
            "fungus flyer", "mushroom turret", "zombie fungus a", "zombie fungus b", "ceiling dropper", "zombie beam miner",
            "electric mage", "flamebearer large" };
        private const float NormalWarnRange = 14f;
        // Ölünce patlayanlar: ölürken yakındaysan uyarı (patlamanın kendisi zaten parry'lenmez: "explosion").
        private static readonly string[] DeathWarnEnemies = { "angry buzzer", "bursting bouncer", "mossman_shaker", "zombie fungus a", "zombie fungus b" };
        // Hiç parry'lenmeyen ve uyarısı da olmayan / ya da tamamen parry'lenmeyen düşmanlar (adın tamamı).
        private static readonly string[] AlwaysUnparryableEnemies =
        {
            "acid walker",      // Durandoo
            "acid flyer",       // Duranda
            "lazy flyer enemy", // Aluba
            "zap bug",          // Charged Lumafly (tahmin)
            "zap cloud",        // Charged Lumafly / elektrik bulutu (log'da görüldü)
            "jellyfish baby",   // Uoma (log'da doğrulandı)
            "worm",             // Goam
            "plant trap",       // Fool Eater (uyarı var)
            "fungus flyer",     // Fungoon (uyarı var)
            "ceiling dropper",  // Belfly (uyarı var)
            "zombie beam miner",// Crystallised Husk (uyarı var)
            "big centipede", "big centipede seg", "big centipede tail",   // Garpede
            "mushroom turret",  // Sporg (uyarı var)
            "gas projectile", "gas projectile quick",   // Fungoon / Sporg mermileri
            "bursting zombie",  // Violent Husk (uyarı var)
            "blow fly"          // Boofly: olduğu yerde durur, saldırmaz; parry'lenmez, uyarısı da yok
        };
        // Husk Guard dalgası: yere indikten sonra bu süre boyunca düşmana bağlı olmayan vuruşlar parry'lenmez.
        private const float GuardWaveTime = 2.0f;
        private static float _guardStompUntil;
        // Husk Guard elindeki sopayla yere vurma: max savrulma.
        private static readonly string[] GuardClubStates = { "Anticipate", "Attack", "Impact", "Impact Left", "Impact Right", "Attack Recoil" };

        // "Buzzer 2", "Fly (4)", "Spitter R(Clone)" -> "buzzer", "fly", "spitter r"
        // Architect modunun koyduğu düşmanlar günlük adıyla geliyor ("[Architect] Mosskin (1a2b3c4d)"):
        // kurallar oyunun iç adlarıyla yazıldığı için günlük adı iç ada çevrilir.
        private static readonly System.Collections.Generic.Dictionary<string, string> JournalToInternal = new System.Collections.Generic.Dictionary<string, string>
        {
            {"crawlid","crawler"},{"vengefly","buzzer"},{"gruzzer","fly"},{"tiktik","climber"},{"aspid hunter","spitter"},
            {"aspid mother","hatcher"},{"aspid hatchling","hatcher baby spawner"},{"goam","worm"},{"wandering husk","zombie runner"},
            {"husk hornhead","zombie hornhead"},{"leaping husk","zombie leaper"},{"husk bully","zombie barger"},{"husk warrior","zombie shield"},
            {"husk guard","zombie guard"},{"entombed husk","grave zombie"},{"baldur","roller"},{"elder baldur","blocker"},
            {"mosscreep","moss walker"},{"mosskin","mossman_runner"},{"volatile mosskin","mossman_shaker"},{"fool eater","plant trap"},
            {"squit","mosquito"},{"obble","fat fly"},{"gulka","plant turret"},{"durandoo","acid walker"},{"duranda","acid flyer"},
            {"aluba","lazy flyer enemy"},{"charged lumafly","zap bug"},{"uoma","jellyfish baby"},{"ooma","jellyfish"},
            {"furious vengefly","angry buzzer"},{"volatile gruzzer","bursting bouncer"},{"violent husk","bursting zombie"},
            {"slobbering husk","spitting zombie"},{"ambloom","fung crawler"},{"fungling","fungoon baby"},{"fungoon","fungus flyer"},
            {"sporg","mushroom turret"},{"fungified husk","zombie fungus a"},{"shrumeling","mushroom baby"},{"shrumal warrior","mushroom roller"},
            {"heavy sentry","ruins sentry fat"},{"husk sentry","ruins sentry"},{"winged sentry","ruins flying sentry"},
            {"lance sentry","ruins flying sentry javelin"},{"soul twister","mage"},
            {"belfly","ceiling dropper"},{"boofly","blow fly"},{"mawlurk","mawlek turret"},{"infected balloon","parasite balloon"},
            {"hive soldier","bee stinger"},{"hive guardian","big bee"},{"volt twister","electric mage"},
            {"grimmkin novice","flamebearer small"},{"grimmkin master","flamebearer med"},{"grimmkin nightmare","flamebearer large"},{"cursor","big centipede"},{"garpede","big centipede"},{"crystallised husk","zombie beam miner"}
        };
        private static string Canon(string n)
        {
            if (string.IsNullOrEmpty(n)) return "";
            string l = n.ToLowerInvariant();
            if (!l.StartsWith("[architect] ")) return l;
            l = l.Substring(12);
            l = System.Text.RegularExpressions.Regex.Replace(l, @"\s*\([0-9a-f]{8}\)\s*$", "").Trim();
            string mapped;
            return JournalToInternal.TryGetValue(l, out mapped) ? mapped : l;
        }

        private static string BaseName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "";
            n = Canon(n);
            n = n.Replace("(clone)", "");
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s*\(\d+\)\s*$", "");
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+\d+\s*$", "");
            return n.Trim().ToLowerInvariant();
        }

        private static PlayMakerFSM FsmByName(GameObject go, string name)
        {
            if (go == null) return null;
            foreach (PlayMakerFSM f in go.GetComponents<PlayMakerFSM>()) if (f != null && f.FsmName == name) return f;
            return null;
        }

        private static bool IsFlukemunga(GameObject g)
        {
            PlayMakerFSM c = FsmByName(g, "Control");
            return c != null && HasState(c, "Roar") && HasState(c, "Resume Walker") && HasState(c, "Charge End");
        }

        private static bool NormalUnparryable(GameObject go)
        {
            if (go == null) return false;
            for (Transform t = go.transform; t != null; t = t.parent)
                if (System.Array.IndexOf(AlwaysUnparryableEnemies, BaseName(t.name)) >= 0) return true;
            if (go.name.StartsWith("Gas Hit Box")) return true;   // Volatile Mosskin / Fungified Husk gaz patlaması (ölünce de)
            HealthManager shm = go.GetComponentInParent<HealthManager>();
            if (shm != null)
            {
                GameObject sg = shm.gameObject;
                if (FsmByName(sg, "Inflater") != null) return true;      // Hwurmp
                if (FsmByName(sg, "Laser Bug") != null) return true;     // Crystal Crawler
                if (IsFlukemunga(sg)) return true;                       // Flukemunga (uyarı var)
            }
            HealthManager hm = go.GetComponentInParent<HealthManager>();
            if (hm == null)
            {
                string gl = go.name.ToLowerInvariant();
                if (gl.Contains("zap") || gl.Contains("pillar")) return true;   // Volt Twister elektriği, Grimmkin Nightmare yerden alev
                Vector2 gp = go.transform.position;
                // Sadece o düşmanın kendi oluşturduğu nesne (yanındaki başka düşmanların mermileri etkilenmez).
                if (NearestWhere(gp, 40f, g => SpawnedBy(go, FsmByName(g, "Electric Mage"), "Gen")) != null) return true;
                if (NearestWhere(gp, 40f, g => { PlayMakerFSM gc = FsmByName(g, "Control"); return gc != null && HasState(gc, "Spawn Pillar") && SpawnedBy(go, gc, "Spawn Pillar"); }) != null) return true;
                return Time.time < _guardStompUntil;   // Husk Guard dalgası
            }
            string b = BaseName(hm.gameObject.name);
            if (b == "mossman_shaker" || b.StartsWith("zombie fungus"))
            {
                PlayMakerFSM f = FsmByName(hm.gameObject, "Fungus Zombie Attack");
                if (f != null && (f.ActiveStateName == "Attack" || f.ActiveStateName == "CD")) return true;   // patlama
            }
            return false;
        }

        // Nesne bu düşmanın şu adımında oluşturduğu nesne mi (ya da onun bir parçası mı)?
        // Oluşturma eylemlerinin "storeObject" alanına bakılır: yakındaki başka düşmanların mermileri karışmaz.
        private static bool SpawnedBy(GameObject hit, PlayMakerFSM f, string state)
        {
            if (hit == null || f == null) return false;
            try
            {
                foreach (var st in f.FsmStates)
                {
                    if (st.Name != state) continue;
                    foreach (var act in st.Actions)
                    {
                        if (act == null) continue;
                        var fi = act.GetType().GetField("storeObject");
                        if (fi == null) continue;
                        var fg = fi.GetValue(act) as HutongGames.PlayMaker.FsmGameObject;
                        GameObject sp = fg != null ? fg.Value : null;
                        if (sp == null) continue;
                        for (Transform t = hit.transform; t != null; t = t.parent) if (t.gameObject == sp) return true;
                    }
                }
            }
            catch (System.Exception) { }
            return false;
        }

        // Ölünce patlayan düşmanlar: öldüğü an (patlamadan önce) yakındaysan uyarı.
        private On.HealthManager.hook_Die _dieHook;
        private void OnEnemyDie(HealthManager hm)
        {
            try
            {
                if (hm == null || !ShinobiOn) return;
                if (System.Array.IndexOf(DeathWarnEnemies, BaseName(hm.gameObject.name)) < 0) return;
                HeroController hero = HeroController.instance;
                if (hero == null || Vector2.Distance(hm.transform.position, hero.transform.position) > NormalWarnRange) return;
                TryWarn(hero, hm.gameObject, "ölünce patlayacak: " + hm.gameObject.name);
            }
            catch (System.Exception) { }
        }

        // Normal düşman parry'si. true dönerse genel "kesme" (InterruptEnemy) yapılmaz.
        private bool NormalEnemyParry(GameObject go, HeroController self, bool pushRight, bool randomBig)
        {
            try
            {
                HealthManager hm = go != null ? go.GetComponentInParent<HealthManager>() : null;
                // Gulka'nın topu: ona geri döner ve vurur.
                GameObject spike = go != null ? FindAncestor(go, "spike ball") : null;
                if (spike != null) { GameManager.instance.StartCoroutine(GulkaBallReturn(spike, self)); return true; }
                // Lance Sentry'nin mızrağı: ona geri döner.
                GameObject jav = go != null ? FindAncestor(go, "sentry javelin") : null;
                if (jav != null && jav.GetComponent<HealthManager>() == null) { GameManager.instance.StartCoroutine(JavelinReturn(jav, self)); return true; }
                // Crystal Hunter'ın kristali / Husk Miner'ın kazması (yakında atan varsa)
                if (hm == null && go != null)
                {
                    Rigidbody2D srb = go.GetComponentInParent<Rigidbody2D>();
                    GameObject shotObj = srb != null ? srb.gameObject : go;
                    // Kingsmould'un bumerangı: hasar vermeden ona geri döner, eline alır.
                    GameObject king = NearestWhere(shotObj.transform.position, 40f, g => { PlayMakerFSM kg = FsmByName(g, "Guard"); return kg != null && kg.ActiveStateName == "Throw" && SpawnedBy(shotObj, kg, "Throw"); });
                    if (king != null) { GameManager.instance.StartCoroutine(KingsmouldCatch(shotObj, king)); return true; }
                    // Spiny Husk'ın dikeni: atan Spiny Husk'a geri döner, 12 hasar.
                    if (shotObj.name.StartsWith("Shot Spine"))
                    {
                        GameObject sp = NearestWhere(shotObj.transform.position, 30f, g => { PlayMakerFSM a = FsmByName(g, "Attack"); return a != null && HasState(a, "Attack Fire"); });
                        if (sp != null) { GameManager.instance.StartCoroutine(ShotReturn(shotObj, sp, 12, "Diken")); return true; }
                    }
                    // Mantis Petra'nın tırpanı: aynı hızla Mantis Petra'ya geri döner, 15 hasar.
                    GameObject petra = NearestWhere(shotObj.transform.position, 40f, g => SpawnedBy(shotObj, FsmByName(g, "Heavy Flyer"), "Shoot"));
                    if (petra != null) { GameManager.instance.StartCoroutine(ShotStraightBack(shotObj, petra, 15, PetraShotSpeedMul)); return true; }
                    GameObject ch = NearestThrower(shotObj.transform.position, "Crystal Flyer", 25f);
                    if (ch != null) { GameManager.instance.StartCoroutine(ShotReturn(shotObj, ch, 15, "Kristal")); return true; }
                    GameObject miner = NearestThrower(shotObj.transform.position, "Zombie Miner", 25f);
                    if (miner != null)
                    {
                        int pdmg = 15;
                        PlayMakerFSM pde = FsmByName(shotObj, "damages_enemy");
                        try { if (pde != null && pde.FsmVariables.GetFsmInt("damageDealt") != null) pdmg = pde.FsmVariables.GetFsmInt("damageDealt").Value; } catch { }
                        if (pdmg <= 0) pdmg = 15;   // kazmanın kendi ayarı 0 çıkıyor
                        GameManager.instance.StartCoroutine(ShotReturn(shotObj, miner, pdmg, "Kazma")); return true;
                    }
                }
                if (hm == null)
                {
                    // Düşmana bağlı olmayan mermi (enfeksiyon atışları vb.): boss savaşı dışında yok olur.
                    if (go != null && !BossFightActive())
                    {
                        Rigidbody2D prb = go.GetComponentInParent<Rigidbody2D>();
                        GameObject shot = prb != null ? prb.gameObject : go;
                        string sl = shot.name.ToLowerInvariant();
                        if (!sl.Contains("lil jellyfish") && !sl.Contains("jar") && !sl.Contains("barrel"))
                        {
                            shot.SetActive(false);
                        }
                    }
                    return false;
                }
                GameObject e = hm.gameObject;
                string b = BaseName(e.name);
                System.Action light = () => { if (!randomBig) GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistLight, 1.0f)); };
                System.Action medium = () => { if (!randomBig) GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMedium, SlashKnockbackSpeed)); };
                System.Action high = () => { if (!randomBig) GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistHigh, FarKnockbackSpeed)); };
                System.Action max = () => { if (!randomBig) GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed)); };

                PlayMakerFSM kgf = FsmByName(e, "Guard");                                                // Kingsmould: yakın vuruş orta
                if (kgf != null && HasState(kgf, "Throw Catch"))
                {
                    string ks = kgf.ActiveStateName;
                    if (ks == "Slash Antic" || ks == "Slash Step" || ks == "Slash1 Attack" || ks == "Slash 2" || ks == "Slash1 Recover") medium();
                    return true;
                }
                PlayMakerFSM gk = FsmByName(e, "Control");                                               // Grimmkin
                if (gk != null && HasState(gk, "Shoot Lv") && HasState(gk, "Tele Out"))
                {
                    string gs = gk.ActiveStateName;
                    if (gs == "Dash Antic" || gs == "Dash")
                    {   // üzerimize dash: biz max savruluruz, o hemen kaybolur (saldırı bitince olduğu gibi)
                        max();
                        if (HasState(gk, "Damage End")) gk.SetState("Damage End");
                    }
                    return true;
                }
                // Lesser Mawlek: yakından vuruşu -> biz hafif savruluruz, o da geriye kayarak kendi toparlanmasıyla durur (içimize girmez).
                PlayMakerFSM lm = FsmByName(e, "Lesser Mawlek");
                if (lm != null)
                {
                    string lms = lm.ActiveStateName;
                    if (lms == "Slash Pause" || lms == "Slash Antic" || lms == "Slash") { light(); PushIntoState(e, lm, self, MawlekSlideSpeed(e, lm), "Slash Recover", "Lesser Mawlek geriye kaydı"); }
                    else if (lms == "Slash Recover" || lms == "Slash recover 2") { light(); PushAway(e, self, MawlekSlideSpeed(e, lm)); }
                    return true;
                }
                if (FsmByName(e, "Mawlek Turret") != null) return true;                     // Mawlurk: gövdesi, değişiklik yok
                if (b.StartsWith("parasite balloon"))                                        // Infected Balloon: biraz geriye gider
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, 0.35f, null)); return true; }
                PlayMakerFSM hop = FsmByName(e, "Hopper");
                if (hop != null && HasState(hop, "Jump Anticipate Alert")) { max(); return true; }   // Great Hopper: max savrulma
                if (hop != null) { HopperTurn(e, hop, self); return true; }                          // Hopper: geri döner, yoluna devam eder
                PlayMakerFSM gmf = FsmByName(e, "Grub Mimic");                                           // Grub Mimic: bize bakarak geriye savrulur, koşma animasyonu sürer
                if (gmf != null) { light(); GameManager.instance.StartCoroutine(PushBackPause(e, gmf, self, 9f, 0.3f, HasState(gmf, "Chase") ? "Chase" : null, "Grub Mimic geriye savruldu")); return true; }
                PlayMakerFSM bee = FsmByName(e, "Bee");                                                  // Hiveling: Vengefly gibi
                if ((bee != null && HasState(bee, "Unfriendly") && HasState(bee, "Chase")) || b.StartsWith("hiveling"))   // + Husk Hive'ın çıkardığı "Hiveling Spawner"
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, FlyerPauseTime, null)); return true; }
                PlayMakerFSM sting = FsmByName(e, "Bee Stinger");                                        // Hive Soldier
                if (sting != null)
                {
                    max();
                    string ss0 = sting.ActiveStateName;
                    if (ss0 == "Zing" || ss0 == "Rear Back") GameManager.instance.StartCoroutine(StingerReverse(e, sting, self));
                    return true;
                }
                PlayMakerFSM bb = FsmByName(e, "Big Bee");                                               // Hive Guardian
                if (bb != null)
                {
                    max();
                    string bs = bb.ActiveStateName;
                    if (bs == "Charge Start" || bs == "Charge R" || bs == "Charge L" || bs == "Charging Start" || bs == "Charging") GuardianBounce(e, bb, self);
                    return true;
                }
                PlayMakerFSM hz = FsmByName(e, "Hive Zombie");                                           // Husk Hive: zıplama saldırısı orta
                if (hz != null)
                {
                    string hzs = hz.ActiveStateName;
                    if (hzs == "Jump Antic" || hzs == "Jump" || hzs == "In Air" || hzs == "Land") medium();
                    return true;
                }
                if (FsmByName(e, "Crazy Hopper") != null) { LoodleBounce(e, self); return true; }       // Loodle: üstümüzden seker
                PlayMakerFSM mt = FsmByName(e, "Mantis");                                                // Mantis Traitor
                if (mt != null && HasState(mt, "DSlash") && HasState(mt, "Feint") && !HasState(mt, "Lords Defeated?"))
                {
                    string mts = mt.ActiveStateName;
                    if (mts == "Attack Antic" || mts == "Feint" || mts == "Speed Check" || mts == "Set Speed" || mts == "Attack 1" || mts == "Attack Swipe")
                    {   // yerden atılma: önümüzde durur, biraz geri çekilip toparlanır (Traitor Lord gibi), biz çok savruluruz
                        high();
                        GameManager.instance.StartCoroutine(PushBackPause(e, mt, self, 9f, 0.2f, "Attack Recover", "Mantis Traitor atılması önümüzde kesildi"));
                    }
                    else if (mts == "Jump" || mts == "DSlash Antic" || mts == "DSlash" || mts == "Land") medium();   // yukarıdan: orta
                    return true;
                }

                if (b == "crawler") { GameManager.instance.StartCoroutine(CrawlerReverse(e, self)); return true; }   // itilme yok: hemen döner
                // Gluttonous Husk: biz hafif savruluruz
                if (dandy0(e) == "gluttonous") { light(); return true; }
                if (dandy0(e) == "gorgeous") { light(); return true; }                                   // Gorgeous Husk: hafif
                if (FsmByName(e, "ZombieShieldControl") != null && (b.Contains("great husk sentry") || b.Contains("great shield zombie"))) { light(); return true; }   // Great Husk Sentry: hafif
                // Cowardly Husk: Husk Dandy gibi geri savrulur, bekleyip tekrar saldırır
                PlayMakerFSM coward = FsmByName(e, "Coward Swipe");
                if (coward != null) { GameManager.instance.StartCoroutine(PushBackPause(e, coward, self, 8f, 0.3f, "Idle", "Cowardly Husk geri savruldu")); return true; }
                // Pilflip: yürürken parry'lenince ters yöne döner; zıplarken parry'lenince biz hafif savruluruz
                PlayMakerFSM pil = FsmByName(e, "Attack");
                if (pil != null && HasState(pil, "Side A") && HasState(pil, "Hop Start"))
                {
                    if (pil.ActiveStateName == "Side A") { Vector3 sc = e.transform.localScale; sc.x = -sc.x; e.transform.localScale = sc; pil.SetState("Side A");  }
                    else light();
                    return true;
                }
                // Flukemon (her parçası): geri savrulur, 0.2 sn bekler, tekrar saldırır
                PlayMakerFSM fluke = null;   // durum makinesinin adı parçaya göre: "Flukeman", "Flukeman Top", "Flukeman Bot"
                foreach (PlayMakerFSM ff in e.GetComponents<PlayMakerFSM>()) if (ff != null && ff.FsmName.StartsWith("Flukeman")) { fluke = ff; break; }
                if (fluke != null)
                {
                    bool legs = e.name.ToLowerInvariant().Contains("bot");   // öldürdükten sonra kalan bacak parçası
                    if (legs) GameManager.instance.StartCoroutine(PushBackPause(e, fluke, self, 16f, 0.4f, "Chase", "Flukemon bacak parçası çok savruldu (" + e.name + ")", 0.2f));
                    else GameManager.instance.StartCoroutine(PushBackPause(e, fluke, self, 8f, 0.25f, "Chase", "Flukemon geri savruldu (" + e.name + ")", 0.2f));
                    return true;
                }
                // Glimback: biz orta savruluruz, o ters yöne yürür
                if (FsmByName(e, "Crawler") != null && FsmByName(e, "Hit Crystals") != null)
                { medium(); GameManager.instance.StartCoroutine(CrawlerReverse(e, self)); return true; }
                // Dirtcarver: yerin altına girer, birkaç saniye sonra başka yerden çıkar
                PlayMakerFSM carver = FsmByName(e, "Centipede");
                if (carver != null && HasState(carver, "Dig") && HasState(carver, "Choose Dig Point"))
                { GameManager.instance.StartCoroutine(CarverDig(e, carver)); return true; }
                // Corpse Creeper: geri savrulur, 0.2 sn bekler, tekrar saldırır
                // Corpse Creeper dirildikten sonra "Zombie Spider" oluyor: geri savrulur, beklemeden kovalamaya devam eder.
                if (b.StartsWith("zombie spider"))
                {
                    PlayMakerFSM zs = FsmByName(e, "Chase");
                    GameManager.instance.StartCoroutine(PushBackPause(e, zs, self, 8f, 0.25f, "Chase", "Dirilmiş Corpse Creeper geri savruldu"));
                    return true;
                }
                if (b.StartsWith("corpse creeper"))
                {
                    PlayMakerFSM cc = FsmByName(e, "Zombie Swipe");
                    if (cc != null) GameManager.instance.StartCoroutine(CreeperPush(e, cc, self));
                    else
                    {
                        // Dirildikten sonra (farklı durum makinesiyle): sadece geri savrulur, beklemeden saldırmaya devam eder.
                        var sb = new StringBuilder();
                        foreach (PlayMakerFSM f in e.GetComponents<PlayMakerFSM>()) sb.Append(f.FsmName).Append("='").Append(f.ActiveStateName).Append("' ");
                        GameManager.instance.StartCoroutine(PushBackPause(e, null, self, 8f, 0.25f, null, "Corpse Creeper geri savruldu (beklemeden)"));
                    }
                    return true;
                }
                // Deepling: ters yöne yürümeye başlar
                if (b.Contains("deepling"))
                { GameManager.instance.StartCoroutine(ReverseWalker(e, self)); return true; }
                // Deephunter: yürürken parry'lenince geri döner (attığı enfeksiyon değişmez)
                PlayMakerFSM dh = FsmByName(e, "Spider");
                if (dh != null && HasState(dh, "Turn Around"))
                {
                    string ds = dh.ActiveStateName;
                    if (ds == "Walk" || ds == "Walk Start" || ds == "Retreat2" || ds == "Retreat Start") { dh.SetState("Turn Around");  }
                    return true;
                }
                // Stalking Devout: ağzıyla yaptığı saldırılar -> biz orta savruluruz
                PlayMakerFSM sd = FsmByName(e, "Slash Spider");
                if (sd != null)
                {
                    string ss = sd.ActiveStateName;
                    if (ss == "A 3" || ss == "A 4" || ss == "A 5") light();                         // 2. ve 3. vuruş: hafif
                    else if (ss.StartsWith("A ") || ss.StartsWith("Attack Antic")) medium();        // 1. vuruş: orta
                    return true;
                }
                if (b == "fung crawler") { GameManager.instance.StartCoroutine(ReverseWalker(e, self)); return true; }   // Ambloom: tersine döner
                if (b == "mushroom roller")                                                                          // Shrumal Warrior
                {
                    PlayMakerFSM mr = FsmByName(e, "Mush Roller");
                    string ms2 = mr != null ? mr.ActiveStateName : "";
                    if (System.Array.IndexOf(ShrumalAttackStates, ms2) >= 0)
                        GameManager.instance.StartCoroutine(PushBackPause(e, mr, self, 8f, 0.3f, "Turn?", "Shrumal Warrior geri yuvarlandı"));
                    return true;
                }
                if (FsmByName(e, "Ruins Sentry Fat") != null)                                                        // Heavy Sentry, Heavy Fool
                {
                    PlayMakerFSM hs = FsmByName(e, "Ruins Sentry Fat");
                    string hss = hs != null ? hs.ActiveStateName : "";
                    // Hücum: orta savrulma, kendisi geriye kayarken hücum bitiş animasyonuyla ("Charge End") normal duruşa geçer.
                    // Zıplayarak vurma ve normal vuruş: hafif savrulma, davranışı değişmez.
                    if (hss.StartsWith("Charge")) { medium(); PushIntoState(e, hs, self, 10f, "Charge End", "Heavy Sentry geri kaydı, hücumu bitiriyor"); }
                    else light();
                    return true;
                }
                if (FsmByName(e, "Flying Sentry Nail") != null)                                                      // Winged Sentry, Winged Fool
                {
                    PlayMakerFSM ws = FsmByName(e, "Flying Sentry Nail");
                    string wss = ws != null ? ws.ActiveStateName : "";
                    if (wss == "Right" || wss == "Left" || wss == "Slash Forward" || wss == "Slash" || wss == "Slash Antic" || wss == "Check Dir")
                        PushIntoState(e, ws, self, 10f, "Slash Recover", "Winged Sentry geriye uçtu, vuruşunu bitiriyor");
                    return true;
                }
                if (b == "ruins flying sentry javelin")                                                              // Lance Sentry
                {
                    PlayMakerFSM ls = FsmByName(e, "Flying Sentry Javelin");
                    string lss = ls != null ? ls.ActiveStateName : "";
                    if (lss == "Charge Antic" || lss == "Charge") { StopTweens(e); GameManager.instance.StartCoroutine(PushBackPause(e, ls, self, 10f, 0.5f, "Idle", "Lance Sentry geriye uçtu")); }
                    return true;
                }
                PlayMakerFSM blob = FsmByName(e, "Blob");                                                             // Mistake
                if (blob != null) { GameManager.instance.StartCoroutine(PushBackPause(e, blob, self, 7f, 0.3f, "Stop Charge", "Mistake geri savruldu")); return true; }
                PlayMakerFSM folly = FsmByName(e, "Control");                                                         // Folly
                if (folly != null && HasState(folly, "Spawn Wait") && HasState(folly, "Chase - In Sight") && !b.Contains("buzzer"))
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, FlyerPauseTime, null)); return true; }
                PlayMakerFSM dandy = FsmByName(e, "Attack");                                                          // Husk Dandy
                if (dandy != null && HasState(dandy, "Prepare Charge") && HasState(dandy, "Swipe")) return true;   // Husk Dandy: değişiklik yok
                PlayMakerFSM mw = FsmByName(e, "Mantis");                                                             // Mantis Warrior
                if (mw != null && HasState(mw, "Lords Defeated?") && !b.Contains("traitor")) { light(); return true; }
                if (b == "zoteling")
                {
                    // Uçan Zoteling geri gider; zıplayan Zoteling'de sadece biz hafif savruluruz.
                    PlayMakerFSM zc = FsmByName(e, "Control");
                    string zs = zc != null ? zc.ActiveStateName : "";
                    if (zs.StartsWith("Buzzer")) GameManager.instance.StartCoroutine(FlyerPause(e, self, FlyerPauseTime, null));
                    else light();
                    return true;
                }                       // Crawlid: yön değiştirir, itilmesi aynı
                if (b == "moss walker") { MossWalkerTurn(e); return true; }                     // Mosscreep: döner
                if (b == "buzzer" || b == "buzzer r" || b == "hatcher baby spawner")           // Vengefly, Aspid yavrusu
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, FlyerPauseTime, null)); return true; }
                if (b == "fly")                                                                // Gruzzer
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, FlyerPauseTime, "Aim")); return true; }
                if (b == "angry buzzer")                                                       // Furious Vengefly: 0.5 sn normale döner
                { GameManager.instance.StartCoroutine(FlyerPause(e, self, 0.5f, null)); return true; }
                if (b == "climber") { GameManager.instance.StartCoroutine(ClimberStun(e)); return true; }   // Tiktik
                // Wandering Husk, Husk Hornhead, Husk Bully: normal savrulma (kendileri geri itilip saldırıya baştan başlar)
                if (b == "zombie runner" || b == "zombie hornhead" || b == "zombie barger") return false;
                if (b == "mossman_runner") { light(); GameManager.instance.StartCoroutine(RollSpin(e, self)); return false; }   // Mosskin: yuvarlanarak geri
                if (b == "zombie leaper" || b.StartsWith("moss knight")) { light(); return true; }
                if (b == "grave zombie") { medium(); return true; }                            // Entombed Husk
                if (b == "zombie guard")
                {
                    PlayMakerFSM f = FsmByName(e, "Zombie Guard");
                    if (f != null && System.Array.IndexOf(GuardClubStates, f.ActiveStateName) >= 0 && !randomBig)
                        GameManager.instance.StartCoroutine(FarKnockbackRoutine(self, pushRight, KnockDistMax, ClawKnockbackSpeed));
                    return true;
                }
                // Baldur ve Elder Baldur'un attığı Baldur ("Spawn Roller v2", aynı durum makinesi): ters yöne döner, sonra açılır.
                // (Elder Baldur'un attığını yok etmek onun "Baldur var mı" sayımını bozuyordu.)
                if (b.StartsWith("roller") || b == "spawn roller v2" || FsmByName(e, "Roller") != null) { GameManager.instance.StartCoroutine(RollerReverse(e)); return true; }   // + Sharp Baldur   // Baldur
                if (b == "mosquito" || FsmByName(e, "Mozzie2") != null) { SquitReflect(e); return true; }   // + Armoured Squit                         // Squit
                if (b == "moss charger")
                {
                    PlayMakerFSM m = MainFsm(e);
                    string ms = m != null ? m.ActiveStateName : "";
                    if (ms == "Charge" || ms == "Emerge") GameManager.instance.StartCoroutine(MossChargerPush(e, self));
                    light();
                    return true;
                }
            }
            catch (System.Exception ex) { Log("HATA (normal düşman parry): " + ex.Message); }
            return false;
        }

        // ---------- Gulka (Plant Turret) topu ----------
        private const float GulkaBallSpeed = 18f;
        private IEnumerator GulkaBallReturn(GameObject ball, HeroController hero)
        {
            // Atan Gulka: topa en yakın Plant Turret
            HealthManager target = null; float best = 30f;
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || hm.hp <= 0 || BaseName(hm.gameObject.name) != "plant turret") continue;
                float d = Vector2.Distance(hm.transform.position, ball.transform.position);
                if (d < best) { best = d; target = hm; }
            }
            // Normalde düşmanlara verdiği hasar (topun kendi "damages_enemy" ayarı)
            int dmg = 10;
            PlayMakerFSM de = FsmByName(ball, "damages_enemy");
            try { if (de != null && de.FsmVariables.GetFsmInt("damageDealt") != null) dmg = de.FsmVariables.GetFsmInt("damageDealt").Value; } catch { }
            if (dmg <= 0) dmg = 15;
            if (target == null) { ball.SetActive(false);  yield break; }
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            foreach (PlayMakerFSM f in ball.GetComponentsInChildren<PlayMakerFSM>()) PauseFsm(f, true);
            foreach (DamageHero dh in ball.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            float speed = Mathf.Max(GulkaBallSpeed, rb != null ? rb.velocity.magnitude : 0f);
            float t0 = Time.time;
            string why = "süre doldu";
            while (ball != null && ball.activeInHierarchy && target != null && Time.time - t0 < 3f)
            {
                Vector2 p = ball.transform.position, tp = target.transform.position;
                Vector2 dir = (tp - p);
                HealthManager first = FirstEnemyTouching(ball, 0.7f);
                if (first != null && first != target) { HitWith(first, ball, dmg, dir.x, "Gulka topu"); why = first.gameObject.name + "'a çarptı"; break; }
                if (dir.magnitude < 0.9f)
                {
                    try
                    {
                        target.Hit(new HitInstance { Source = ball, AttackType = AttackTypes.Generic, DamageDealt = dmg,
                            Direction = dir.x >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true });
                    }
                    catch (System.Exception ex) { Log("HATA (Gulka topu): " + ex.Message); }
                    why = target.gameObject.name + "'a çarptı (" + dmg + " hasar)";
                    break;
                }
                Vector2 v = dir.normalized * speed;
                if (rb != null) rb.velocity = v; else ball.transform.position = p + v * Time.deltaTime;
                yield return null;
            }
            if (ball != null) ball.SetActive(false);
        }


        // Düşman Knight'tan uzağa itilir ve hemen verilen toparlanma adımına geçer: o adımın kendi animasyonu oynar,
        // kendi yavaşlaması da itilmeyi yumuşakça durdurur (Winged Sentry "Slash Recover", Heavy Sentry "Charge End").
        // Durum makinesi durdurulmadığı için saldırının ortasında donup kalmaz.
        private void PushIntoState(GameObject e, PlayMakerFSM f, HeroController hero, float speed, string state, string what)
        {
            if (e == null || f == null || !HasState(f, state)) return;
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            float dir = hero != null && hero.transform.position.x > e.transform.position.x ? -1f : 1f;
            f.SetState(state);
            if (rb != null) rb.velocity = new Vector2(dir * speed, 0f);
        }

        // Lesser Mawlek'in kendi atılma hızı ("Slash Speed"): geriye kayarken bu hızla başlar, kendi yavaşlamasıyla durur.
        private static float MawlekSlideSpeed(GameObject e, PlayMakerFSM f)
        {
            float sp = 0f;
            try { var fv = f.FsmVariables.GetFsmFloat("Slash Speed"); if (fv != null) sp = Mathf.Abs(fv.Value); } catch { }
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (rb != null) sp = Mathf.Max(sp, Mathf.Abs(rb.velocity.x));
            return sp > 1f ? sp : 25f;
        }

        private void PushAway(GameObject e, HeroController hero, float speed)
        {
            Rigidbody2D rb = e != null ? e.GetComponent<Rigidbody2D>() : null;
            if (rb == null || hero == null) return;
            rb.velocity = new Vector2((hero.transform.position.x > e.transform.position.x ? -1f : 1f) * speed, rb.velocity.y);
        }

        // Hopper: Knight'a doğru gidiyorsa ters yöne döner, yoluna orada devam eder.
        // Havadaysa oyunun kendi dönüş adımı ("Flip": yönü, çizimi ve hızı çevirir), yerdeyse kendi dönme adımı ("Turn R/L").
        private void HopperTurn(GameObject e, PlayMakerFSM f, HeroController hero)
        {
            if (hero == null) return;
            bool goRight = e.transform.position.x > hero.transform.position.x;
            var mr = f.FsmVariables.GetFsmBool("Moving Right");
            string st0 = f.ActiveStateName;
            if (mr != null && mr.Value == goRight) {  return; }
            if (st0 == "In Air" && HasState(f, "Flip"))
            {
                f.SetState("Flip");
                Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2((goRight ? 1f : -1f) * Mathf.Abs(rb.velocity.x), rb.velocity.y);
            }
            else if (HasState(f, goRight ? "Turn R" : "Turn L")) f.SetState(goRight ? "Turn R" : "Turn L");
            // Dolaşma alanının dışındaysa (Architect'in koyduğu Hopper'larda alan başka yerde kalıyor) indiğinde alana doğru
            // geri dönüyordu: o zaman alan kuralı kapatılır, yeni yönünde devam eder.
            var roam = f.FsmVariables.GetFsmBool("Roams Within Range");
            var rmin = f.FsmVariables.GetFsmFloat("Range Min");
            var rmax = f.FsmVariables.GetFsmFloat("Range Max");
            float hx0 = e.transform.position.x;
            if (roam != null && roam.Value && rmin != null && rmax != null && (hx0 < rmin.Value - 1f || hx0 > rmax.Value + 1f))
            { roam.Value = false;  }
        }

        // Hive Soldier: dikenle atılırken parry'lenince geldiği yöne doğru atılmaya devam eder, sonra kendi toparlanmasına geçer.
        private const float StingerBackTime = 0.5f;
        private IEnumerator StingerReverse(GameObject e, PlayMakerFSM f, HeroController hero)
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (rb == null || hero == null) yield break;
            Vector2 away = ((Vector2)(e.transform.position - hero.transform.position)).normalized;
            Vector2 v = rb.velocity.sqrMagnitude > 4f ? -rb.velocity : away * 20f;
            if (Vector2.Dot(v, away) < 0f) v = Vector2.Reflect(v, away);
            PauseFsm(f, true);
            e.transform.rotation = Quaternion.Euler(0f, 0f, e.transform.eulerAngles.z + 180f);
            float t0 = Time.time;
            while (e != null && Time.time - t0 < StingerBackTime) { rb.velocity = v; yield return null; }
            if (e == null || f == null) yield break;
            PauseFsm(f, false);
            if (HasState(f, "Recover")) f.SetState("Recover");
        }

        // Hive Guardian: hücumu parry'lenince duvara çarpmış gibi bizden seker ve hücumuna devam eder.
        private void GuardianBounce(GameObject e, PlayMakerFSM f, HeroController hero)
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (rb == null || hero == null) return;
            Vector2 n = ((Vector2)(e.transform.position - hero.transform.position)).normalized;
            Vector2 v = rb.velocity;
            if (Vector2.Dot(v, n) < 0f) v = Vector2.Reflect(v, n);
            if (v.sqrMagnitude < 1f) v = n * 15f;
            var ang = f.FsmVariables.GetFsmFloat("Angle");
            if (ang != null) ang.Value = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (HasState(f, "Charge Start")) f.SetState("Charge Start");
            rb.velocity = v;
        }

        // Loodle: havadayken parry'lenince duvardan sekmiş gibi üstümüzden seker.
        private void LoodleBounce(GameObject e, HeroController hero)
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (rb == null || hero == null) return;
            Vector2 n = ((Vector2)(e.transform.position - hero.transform.position)).normalized;
            Vector2 v = rb.velocity;
            if (v.sqrMagnitude < 9f) {  return; }
            if (Vector2.Dot(v, n) < 0f) v = Vector2.Reflect(v, n);
            rb.velocity = v;
        }

        private static readonly string[] ShrumalAttackStates = { "Roll Antic", "Roll", "Roll Last", "Jump Antic", "Jump", "In Air" };

        // Genel: düşmanın aklı kısa süre durur, Knight'tan uzağa (yavaşlayarak) savrulur, sonra verilen adımdan devam eder.
        // "Attack" durum makineli saray hizmetkârları: Husk Dandy (Swipe), Gluttonous (State 1), Gorgeous (In Air)
        private static string dandy0(GameObject e)
        {
            PlayMakerFSM a = FsmByName(e, "Attack");
            if (a == null || !HasState(a, "Prepare Charge")) return "";
            if (HasState(a, "Swipe")) return "dandy";
            if (HasState(a, "In Air")) return "gorgeous";
            if (HasState(a, "State 1")) return "gluttonous";
            return "";
        }

        private readonly System.Collections.Generic.HashSet<GameObject> _pushing = new System.Collections.Generic.HashSet<GameObject>();
        private IEnumerator PushBackPause(GameObject e, PlayMakerFSM f, HeroController hero, float speed, float time, string resume, string what, float extraWait = 0f)
        {
            // Aynı düşman zaten savruluyorsa (art arda parry) ikinci savrulma başlatılmaz: üst üste binince durum makinesi
            // yanlış anda açılıp düşman takılıyordu (Shrumal Warrior).
            if (e == null || !_pushing.Add(e)) yield break;
            try { yield return PushBackPauseInner(e, f, hero, speed, time, resume, what, extraWait); }
            finally { _pushing.Remove(e); }
        }

        private IEnumerator PushBackPauseInner(GameObject e, PlayMakerFSM f, HeroController hero, float speed, float time, string resume, string what, float extraWait)
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            PauseFsm(f, true);
            float dir = hero != null && hero.transform.position.x > e.transform.position.x ? -1f : 1f;
            float t0 = Time.time;
            while (e != null && Time.time - t0 < time)
            {
                float k = 1f - (Time.time - t0) / time;
                if (rb != null) rb.velocity = new Vector2(dir * speed * k, rb.velocity.y);
                yield return null;
            }
            if (e == null) yield break;
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            if (extraWait > 0f) { yield return new WaitForSeconds(extraWait); if (e == null) yield break; }
            PauseFsm(f, false);
            if (f != null && resume != null && HasState(f, resume)) f.SetState(resume);
        }


        // Dirtcarver: oyunun kendi kazma adımıyla yerin altına girer; birkaç saniye içinde çıkmazsa çıkması tetiklenir.
        private IEnumerator CarverDig(GameObject e, PlayMakerFSM c)
        {
            c.SetState("Dig");
            yield return new WaitForSeconds(3f);
            if (e != null && c != null && c.ActiveStateName == "Dormant") { c.SendEvent("EMERGE");  }
        }

        // Walker bileşeniyle yürüyenler (Ambloom, Deepling): Knight'tan uzağa yürür.
        // Çizimi çevirmek yetmiyordu (yürüme yönü Walker'ın içinde): Walker'a yön verilir, tutmazsa ters yön denenir.
        private static bool _walkerLogged;
        private IEnumerator ReverseWalker(GameObject e, HeroController hero)
        {
            Component w = null;
            foreach (Component c in e.GetComponents<Component>()) if (c != null && c.GetType().Name == "Walker") { w = c; break; }
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            bool goRight = hero == null || e.transform.position.x > hero.transform.position.x;
            if (w == null) {  yield break; }
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            if (!_walkerLogged)
            {
                _walkerLogged = true;
                var sb = new StringBuilder("Walker yöntemleri:");
                foreach (var m in w.GetType().GetMethods(flags)) if (m.DeclaringType == w.GetType()) { sb.Append(' ').Append(m.Name).Append('('); foreach (var pp in m.GetParameters()) sb.Append(pp.ParameterType.Name).Append(' '); sb.Append(')'); }
                sb.Append(" | alanlar:");
                foreach (var fi in w.GetType().GetFields(flags)) sb.Append(' ').Append(fi.Name).Append('=').Append(fi.GetValue(w));
            }
            if (rb != null && (goRight ? rb.velocity.x > 0.1f : rb.velocity.x < -0.1f)) {  yield break; }
            var cf = w.GetType().GetMethod("BeginTurning", flags) ?? w.GetType().GetMethod("ChangeFacing", flags);   // BeginTurning: oyunun kendi dönüşü (çizim de döner)
            string how = "yok";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int facing = (goRight ? 1 : -1) * (attempt == 0 ? 1 : -1);
                if (cf != null && cf.GetParameters().Length == 1 && cf.GetParameters()[0].ParameterType == typeof(int))
                {
                    try
                    {
                        var ctc = w.GetType().GetMethod("ClearTurnCooldown", flags);
                        if (ctc != null && ctc.GetParameters().Length == 0) ctc.Invoke(w, null);   // dönüş bekleme süresi engellemesin
                        cf.Invoke(w, new object[] { facing }); how = cf.Name + "(" + facing + ")";
                    }
                    catch (System.Exception ex) { how = cf.Name + " hata: " + ex.Message; }
                }
                yield return new WaitForSeconds(0.6f);   // dönme animasyonu bitsin
                if (e == null) yield break;
                if (rb == null || (goRight ? rb.velocity.x > 0.1f : rb.velocity.x < -0.1f)) break;
            }
            bool ok = rb == null || (goRight ? rb.velocity.x > 0.1f : rb.velocity.x < -0.1f);
        }

        // Corpse Creeper: geri savrulur, "normale dönme" animasyonu (Cooldown) oynar, 0.2 sn sonra tekrar saldırır.
        private IEnumerator CreeperPush(GameObject e, PlayMakerFSM cc, HeroController hero)
        {
            yield return PushBackPause(e, cc, hero, 8f, 0.25f, "Cooldown", "Corpse Creeper geri savruldu");
            float t0 = Time.time;
            while (e != null && cc != null && cc.ActiveStateName == "Cooldown" && Time.time - t0 < 1.5f) yield return null;
            if (e == null || cc == null) yield break;
            yield return new WaitForSeconds(0.2f);
            if (e != null && cc != null && (cc.ActiveStateName == "Idle" || cc.ActiveStateName == "Cooldown")) cc.SetState("Ready");
        }

        // Crystal Hunter'ın kristali (15 hasar) ve Husk Miner'ın kazması: atana geri döner.
        private GameObject NearestThrower(Vector2 at, string fsmName, float maxDist)
        {
            GameObject best = null; float bd = maxDist;
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || hm.hp <= 0 || FsmByName(hm.gameObject, fsmName) == null) continue;
                float d = Vector2.Distance(hm.transform.position, at);
                if (d < bd) { bd = d; best = hm.gameObject; }
            }
            return best;
        }

        // Mantis Petra'nın tırpanı: geldiği yönün tersine düz gider (aşağıdan parry'lenirse aşağı), biraz hızlanarak.
        // Yolda ilk değdiği düşmana (Mantis Petra ya da başkası) 15 hasar.
        private const float PetraShotSpeedMul = 1.3f;
        private IEnumerator ShotStraightBack(GameObject shot, GameObject owner, int damage, float speedMul)
        {
            Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
            foreach (DamageHero dh in shot.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            Vector2 p0 = shot.transform.position;
            yield return null;   // hızını ölçmek için bir kare (hasar vermez)
            if (shot == null) yield break;
            Vector2 v0 = rb != null && rb.velocity.sqrMagnitude > 1f ? rb.velocity : ((Vector2)shot.transform.position - p0) / Mathf.Max(Time.deltaTime, 0.0001f);
            foreach (PlayMakerFSM f in shot.GetComponentsInChildren<PlayMakerFSM>()) PauseFsm(f, true);
            HeroController hc = HeroController.instance;
            if (v0.sqrMagnitude < 1f && hc != null) v0 = ((Vector2)hc.transform.position - (Vector2)shot.transform.position).normalized * 15f;
            Vector2 v = -v0 * speedMul;
            HealthManager ohm = owner != null ? owner.GetComponent<HealthManager>() : null;
            float t0 = Time.time;
            while (shot != null && shot.activeInHierarchy && Time.time - t0 < 2.5f)
            {
                // Yolda ilk değdiği düşman (Mantis Petra ya da başka biri) 15 hasar alır, tırpan orada kaybolur.
                HealthManager hitHm = FirstEnemyTouching(shot, 0.8f);
                if (hitHm == null && ohm != null && ohm.hp > 0 && Vector2.Distance(shot.transform.position, owner.transform.position) < 1f) hitHm = ohm;
                if (hitHm != null)
                {
                    try { hitHm.Hit(new HitInstance { Source = shot, AttackType = AttackTypes.Generic, DamageDealt = damage, Direction = v.x >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true }); }
                    catch (System.Exception ex) { Log("HATA (tırpan): " + ex.Message); }
                    break;
                }
                if (rb != null) rb.velocity = v; else shot.transform.position = (Vector2)shot.transform.position + v * Time.deltaTime;
                yield return null;
            }
            if (shot != null) shot.SetActive(false);
        }

        // Kingsmould'un bumerangı: parry'lenince hasar vermeden geri döner; değince Kingsmould onu yakalar (kendi yakalama adımı).
        private const float KingArcRadius = 1.75f;  // Knight bulunamazsa kullanılan yarıçap
        private const float KingArcMinRadius = 0.6f; // en küçük yarım daire
        private const float KingArcMaxRadius = 2.5f; // en büyük yarım daire
        private const float KingArcClear = 0.6f;     // yolun Knight'ın başının üstünden geçme payı
        private const float KingMinSpeed = 16f;      // yarım daireye ve dönüşe en az bu hızla
        private const float KingReturnAccel = 35f;   // dönüşte hızlanma (birim/sn²)
        private const float KingReturnMaxMul = 1.8f; // en fazla bu kat hıza çıkar
        private const float KingArcEase = 0.6f;     // dönüşün ortasında yavaşlama (kendi dönüşü gibi): uçlarda tam hız, tepede 1/4
        private IEnumerator KingsmouldCatch(GameObject shot, GameObject king)
        {
            Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
            foreach (DamageHero dh in shot.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            Vector2 p0 = shot.transform.position;
            yield return null;   // kendi hızını ölçmek için bir kare (hasar vermez)
            if (shot == null) yield break;
            Vector2 v0 = rb != null && rb.velocity.sqrMagnitude > 1f ? rb.velocity : ((Vector2)shot.transform.position - p0) / Mathf.Max(Time.deltaTime, 0.0001f);
            foreach (PlayMakerFSM f in shot.GetComponentsInChildren<PlayMakerFSM>()) PauseFsm(f, true);
            // Dönüş noktasına yakınken neredeyse durmuş oluyor: en az KingMinSpeed hızla devam eder,
            // yönü de hızından değil konumundan anlaşılır (Knight'tan daha uzaktaysa dönüşte sayılır).
            float speed = Mathf.Clamp(v0.magnitude, KingMinSpeed, 40f);
            bool returning;
            HeroController hk0 = HeroController.instance;
            if (king != null && v0.magnitude < KingMinSpeed && hk0 != null)
                returning = Mathf.Abs(shot.transform.position.x - king.transform.position.x) > Mathf.Abs(hk0.transform.position.x - king.transform.position.x);
            else
                returning = king != null && Vector2.Dot(v0, (Vector2)king.transform.position - (Vector2)shot.transform.position) > 0f;
            if (returning && king != null)
            {
                // Tersten: geriye seker, kendi dönüşü gibi yukarı doğru yarım daire çizer, sonra Kingsmould'a gider.
                float dirX = Mathf.Sign(shot.transform.position.x - king.transform.position.x);
                float ang = dirX > 0f ? 0f : 180f;              // başlangıç: Kingsmould'dan uzağa
                float turn = dirX > 0f ? 1f : -1f;               // sağa gidiyorsa saat yönünün tersine (yukarı), sola gidiyorsa saat yönünde
                // Yarım dairenin boyu sabit değil: tepesinden Kingsmould'a giden düz yol Knight'ın başının üstünden
                // geçecek kadar yükselir. Knight yakındaysa daha çok, uzaktaysa daha az (yol daha yavaş alçalır).
                float r = KingArcRadius;
                HeroController hk = HeroController.instance;
                Collider2D hcol = hk != null ? hk.GetComponent<Collider2D>() : null;
                if (hk != null)
                {
                    float xS = shot.transform.position.x, yS = shot.transform.position.y;
                    float xK = king.transform.position.x, yK = king.transform.position.y;
                    float xH = hk.transform.position.x;
                    float need = (hcol != null ? hcol.bounds.max.y : hk.transform.position.y + 0.7f) + KingArcClear;   // başın üstü + pay
                    float f = Mathf.Abs(xK - xS) > 0.01f ? (xH - xS) / (xK - xS) : 0f;   // Knight yolun neresinde (0 = tepe, 1 = Kingsmould)
                    float yT;
                    if (f <= 0f) yT = need;                       // Knight yolun arkasında: sadece başını geçsin
                    else if (f >= 0.98f) yT = yS + 2f * KingArcMinRadius;   // Knight Kingsmould'un ötesinde: yol ona değmez
                    else yT = (need - yK * f) / (1f - f);
                    r = Mathf.Clamp((yT - yS) / 2f, KingArcMinRadius, KingArcMaxRadius);
                }
                // Kendi dönüşü gibi: yarım daireye kendi hızıyla girer, tepeye doğru yavaşlar, sonra yine hızlanır.
                float T = Mathf.PI * r * (1f + KingArcEase) / speed;   // yarım dairenin süresi
                float ta = Time.time;
                while (shot != null && shot.activeInHierarchy && Time.time - ta < T)
                {
                    float u = (Time.time - ta) / T;
                    float th = Mathf.PI * (u + KingArcEase * Mathf.Sin(2f * Mathf.PI * u) / (2f * Mathf.PI));    // 0..π
                    float w = Mathf.PI * (1f + KingArcEase * Mathf.Cos(2f * Mathf.PI * u)) / T;                    // açısal hız
                    float a = ang * Mathf.Deg2Rad + turn * th;
                    Vector2 va = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (w * r);
                    if (rb != null) rb.velocity = va; else shot.transform.position = (Vector2)shot.transform.position + va * Time.deltaTime;
                    yield return null;
                }
            }
            // Yarım dairenin tepesinden doğrudan Kingsmould'a gider.
            // Kendi dönüşündeki gibi Kingsmould'a giderken hızlanır.
            float t0 = Time.time;
            float spNow = speed;
            while (shot != null && shot.activeInHierarchy && king != null && Time.time - t0 < 3f)
            {
                Vector2 p = shot.transform.position, d = (Vector2)king.transform.position - p;
                if (d.magnitude < 1f) break;
                spNow = Mathf.Min(speed * KingReturnMaxMul, spNow + KingReturnAccel * Time.deltaTime);
                Vector2 v = d.normalized * spNow;
                if (rb != null) rb.velocity = v; else shot.transform.position = p + v * Time.deltaTime;
                yield return null;
            }
            if (shot != null) shot.SetActive(false);
            PlayMakerFSM kg = king != null ? FsmByName(king, "Guard") : null;
            if (kg != null && kg.ActiveStateName == "Throw") { kg.SendEvent("CAUGHT");  }
        }

        // Geri gönderilen şeyler: yolda ilk değdiği düşman (gövdesi). Alt nesnelerdeki algılama alanları sayılmaz.
        private static HealthManager FirstEnemyTouching(GameObject proj, float radius)
        {
            if (proj == null) return null;
            HealthManager own = proj.GetComponentInParent<HealthManager>();
            foreach (Collider2D c in Physics2D.OverlapCircleAll(proj.transform.position, radius))
            {
                if (c == null || !c.enabled) continue;
                HealthManager h = c.GetComponentInParent<HealthManager>();
                if (h == null || h == own || h.hp <= 0 || !h.isActiveAndEnabled) continue;
                if (c.gameObject != h.gameObject && c.isTrigger) continue;
                return h;
            }
            return null;
        }
        // Değdiği düşmana hasar verir (log'a yazar).
        private void HitWith(HealthManager h, GameObject src, int dmg, float dirX, string what)
        {
            if (h == null) return;
            if (dmg > 0)
                try { h.Hit(new HitInstance { Source = src, AttackType = AttackTypes.Generic, DamageDealt = dmg, Direction = dirX >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true }); }
                catch (System.Exception ex) { Log("HATA (" + what + "): " + ex.Message); }
        }

        private static GameObject NearestWhere(Vector2 at, float maxDist, System.Func<GameObject, bool> ok)
        {
            GameObject best = null; float bd = maxDist;
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || hm.hp <= 0 || !ok(hm.gameObject)) continue;
                float d = Vector2.Distance(hm.transform.position, at);
                if (d < bd) { bd = d; best = hm.gameObject; }
            }
            return best;
        }

        private IEnumerator ShotReturn(GameObject shot, GameObject target, int damage, string what, bool sameSpeed = false)
        {
            Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
            foreach (DamageHero dh in shot.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            // Kazma Knight'a değdiği an "Impact" (kırılma) adımına geçiyor: uçuş adımına geri alınır, kırılma animasyonu iptal edilir.
            foreach (PlayMakerFSM f in shot.GetComponentsInChildren<PlayMakerFSM>())
            {
                if (f != null && f.ActiveStateName == "Impact" && HasState(f, "In Air")) f.SetState("In Air");
                PauseFsm(f, true);
            }
            tk2dSpriteAnimator san = shot.GetComponent<tk2dSpriteAnimator>();
            if (san != null && san.CurrentClip != null && san.CurrentClip.name.Contains("Shatter") && san.GetClipByName("Attack Projectile") != null) san.Play("Attack Projectile");
            foreach (Renderer r in shot.GetComponentsInChildren<Renderer>()) r.enabled = true;
            Vector2 v0 = rb != null ? rb.velocity : Vector2.zero;
            // Kazma geri dönerken kendi etrafında döner (gelirken olduğu gibi); kristal uçuş yönüne bakar.
            bool spin = what == "Kazma";
            float spinRate = rb != null && Mathf.Abs(rb.angularVelocity) > 90f ? Mathf.Abs(rb.angularVelocity) : 720f;
            if (spin && rb != null) rb.angularVelocity = 0f;
            float offset = v0.sqrMagnitude > 0.1f ? shot.transform.eulerAngles.z - Mathf.Atan2(v0.y, v0.x) * Mathf.Rad2Deg : 0f;
            float speed = sameSpeed && v0.magnitude > 1f ? v0.magnitude : Mathf.Max(18f, v0.magnitude);   // tırpan: geldiği hızla
            HealthManager thm = target.GetComponent<HealthManager>();
            float t0 = Time.time;
            while (shot != null && shot.activeInHierarchy && target != null && Time.time - t0 < 3f)
            {
                Vector2 p = shot.transform.position, d = (Vector2)target.transform.position - p;
                HealthManager first = FirstEnemyTouching(shot, 0.7f);
                if (first != null && first != thm) { HitWith(first, shot, damage, d.x, what); break; }
                if (d.magnitude < 0.8f)
                {
                    if (damage > 0 && thm != null)
                        try { thm.Hit(new HitInstance { Source = shot, AttackType = AttackTypes.Generic, DamageDealt = damage, Direction = d.x >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true }); }
                        catch (System.Exception ex) { Log("HATA (" + what + "): " + ex.Message); }
                    break;
                }
                Vector2 v = d.normalized * speed;
                if (rb != null) rb.velocity = v; else shot.transform.position = p + v * Time.deltaTime;
                if (spin) shot.transform.Rotate(0f, 0f, -Mathf.Sign(v.x == 0f ? 1f : v.x) * spinRate * Time.deltaTime);
                else if (v0.sqrMagnitude > 0.1f) shot.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + offset);
                yield return null;
            }
            if (shot != null) shot.SetActive(false);
        }


        // iTween ile hareket eden düşmanların (Lance Sentry hücumu) hareketini durdurur.
        private static void StopTweens(GameObject e)
        {
            foreach (Component c in e.GetComponents<Component>())
                if (c != null && c.GetType().Name == "iTween") UnityEngine.Object.Destroy(c);
        }

        // ---------- Lance Sentry mızrağı: atana geri döner ----------
        private const float JavelinReturnSpeed = 22f;
        private IEnumerator JavelinReturn(GameObject jav, HeroController hero)
        {
            HealthManager target = null; float best = 40f;
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || hm.hp <= 0 || BaseName(hm.gameObject.name) != "ruins flying sentry javelin") continue;
                float d = Vector2.Distance(hm.transform.position, jav.transform.position);
                if (d < best) { best = d; target = hm; }
            }
            Rigidbody2D rb = jav.GetComponent<Rigidbody2D>();
            foreach (DamageHero dh in jav.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            foreach (PlayMakerFSM f in jav.GetComponentsInChildren<PlayMakerFSM>()) PauseFsm(f, true);
            if (target == null) { jav.SetActive(false);  yield break; }
            // Çizimin uçuş yönüne göre açısı korunur
            Vector2 v0 = rb != null ? rb.velocity : Vector2.zero;
            float offset = v0.sqrMagnitude > 0.1f ? jav.transform.eulerAngles.z - Mathf.Atan2(v0.y, v0.x) * Mathf.Rad2Deg : 0f;
            // Hasar: mızrağın düşmanlara normalde verdiği ("damages_enemy" ayarı varsa), yoksa 15
            int dmg = 15;
            PlayMakerFSM de = FsmByName(jav, "damages_enemy");
            try { if (de != null && de.FsmVariables.GetFsmInt("damageDealt") != null) dmg = de.FsmVariables.GetFsmInt("damageDealt").Value; } catch { }
            if (dmg <= 0) dmg = 15;
            float t0 = Time.time;
            string why = "süre doldu";
            while (jav != null && jav.activeInHierarchy && target != null && Time.time - t0 < 3f)
            {
                Vector2 p = jav.transform.position, d = (Vector2)target.transform.position - p;
                HealthManager first = FirstEnemyTouching(jav, 0.7f);
                if (first != null && first != target) { HitWith(first, jav, dmg, d.x, "Mızrak"); why = first.gameObject.name + "'a çarptı"; break; }
                if (d.magnitude < 0.8f)
                {
                    try { target.Hit(new HitInstance { Source = jav, AttackType = AttackTypes.Generic, DamageDealt = dmg, Direction = d.x >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true }); }
                    catch (System.Exception ex) { Log("HATA (mızrak): " + ex.Message); }
                    why = target.gameObject.name + "'a çarptı (" + dmg + " hasar)";
                    break;
                }
                Vector2 v = d.normalized * JavelinReturnSpeed;
                if (rb != null) rb.velocity = v; else jav.transform.position = p + v * Time.deltaTime;
                jav.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + offset);
                yield return null;
            }
            if (jav != null) jav.SetActive(false);
        }

        private const float FlyerPauseTime = 0.45f;   // Vengefly / Gruzzer / Aspid yavrusu: parry'den sonra duraksama
        private const float FlyerPushSpeed = 9f;      // duraksarken geri savrulma hızı (yavaşlayarak durur)

        private IEnumerator FlyerPause(GameObject e, HeroController hero, float time, string resumeState)
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            PlayMakerFSM[] fsms = e.GetComponents<PlayMakerFSM>();
            foreach (PlayMakerFSM f in fsms) PauseFsm(f, true);
            Vector2 dir = hero != null ? (Vector2)(e.transform.position - hero.transform.position) : Vector2.right;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            dir.Normalize();
            float t0 = Time.time;
            while (e != null && Time.time - t0 < time)
            {
                float k = 1f - (Time.time - t0) / time;
                if (rb != null) rb.velocity = dir * FlyerPushSpeed * k * k;
                yield return null;
            }
            if (e == null) yield break;
            if (rb != null) rb.velocity = Vector2.zero;
            foreach (PlayMakerFSM f in fsms)
            {
                PauseFsm(f, false);
                if (resumeState != null && f != null && f.FsmName == "Bouncer Control") f.SetState(resumeState);
            }
        }

        // Crawlid / Glimback: oyunun kendi dönüşüyle (dönme animasyonu dahil) hemen Knight'tan uzağa döner.
        // Yürüme yönü çizimin yönünden geliyor (spriteFacesLeft: boyut x > 0 = sola yürür). Oyun duvara çarpınca
        // "shouldTurn" ile dönüyor ama iki dönüş arasında 1 sn bekleme var ("nextTurnTime"): bazen dönmemesinin sebebi buydu.
        private bool RequestWalkTurn(PlayMakerFSM f)
        {
            try
            {
                var st = f.Fsm.ActiveState;
                if (st == null) return false;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                foreach (var act in st.Actions)
                {
                    if (act == null || act.GetType().Name != "WalkLeftRight") continue;
                    var nt = act.GetType().GetField("nextTurnTime", flags);
                    if (nt != null && nt.FieldType == typeof(float)) nt.SetValue(act, 0f);
                    var sh = act.GetType().GetField("shouldTurn", flags);
                    if (sh != null && sh.FieldType == typeof(bool)) { sh.SetValue(act, true); return true; }
                }
            }
            catch (System.Exception) {  }
            return false;
        }

        private IEnumerator CrawlerReverse(GameObject e, HeroController hero)
        {
            PlayMakerFSM f = FsmByName(e, "Crawler");
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (f == null || hero == null) yield break;
            bool goRight = e.transform.position.x > hero.transform.position.x;   // Knight'tan uzağa
            bool movingRight = e.transform.localScale.x < 0f;                    // çizim sola bakıyor: boyut x < 0 = sağa yürür
            if (movingRight == goRight) {  yield break; }
            string how = RequestWalkTurn(f) ? "oyunun dönüşü" : "dönüş isteği bulunamadı";
            float t0 = Time.time;
            while (e != null && Time.time - t0 < 0.5f && ((e.transform.localScale.x < 0f) != goRight)) yield return null;
            if (e == null) yield break;
            if ((e.transform.localScale.x < 0f) != goRight)
            {
                // Oyun dönmediyse çizim doğrudan çevrilir (yürüme yönü çizimden geldiği için o yöne yürür).
                Vector3 sc = e.transform.localScale; sc.x = -sc.x; e.transform.localScale = sc;
                how += " + çizim çevrildi";
            }
            yield return new WaitForSeconds(0.1f);
            if (e == null) yield break;
            bool ok = rb == null || (goRight ? rb.velocity.x > 0.1f : rb.velocity.x < -0.1f);
        }

        private void MossWalkerTurn(GameObject e)
        {
            PlayMakerFSM f = FsmByName(e, "Moss Walker");
            if (f == null) return;
            string st = f.ActiveStateName;
            if (st == "Walking" || st == "Walk Start") { f.SetState("Turn");  }

        }

        private const float ClimberStunTime = 0.2f;
        // Tiktik: vurulduğunda oynadığı "Stun" animasyonuna girer, olduğu yerde 0.2 sn kalır, sonra yoluna devam eder.
        // Climber bileşenini kapatmak hareketini tamamen durduruyordu (tekrar açılınca devam etmiyordu): bileşen açık kalır,
        // bu sürede sadece yeri sabitlenir.
        private IEnumerator ClimberStun(GameObject e)
        {
            tk2dSpriteAnimator an = e.GetComponent<tk2dSpriteAnimator>();
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            string prevClip = an != null && an.CurrentClip != null ? an.CurrentClip.name : null;
            Vector3 pos = e.transform.position;
            Quaternion rot = e.transform.rotation;
            Vector2 v0 = rb != null ? rb.velocity : Vector2.zero;   // Tiktik hızını sadece köşede ayarlıyor: sonra geri verilmeli
            if (an != null && an.GetClipByName("Stun") != null) an.Play("Stun");
            float t0 = Time.time;
            while (e != null && Time.time - t0 < ClimberStunTime)
            {
                e.transform.position = pos; e.transform.rotation = rot;
                if (rb != null) rb.velocity = Vector2.zero;
                yield return null;
            }
            if (e == null) yield break;
            if (rb != null) rb.velocity = v0;
            if (an != null) an.Play(prevClip != null && an.GetClipByName(prevClip) != null ? prevClip : "Walk");
        }

        // Mosskin: geri savrulurken kendi atılma animasyonundaki yuvarlanma kareleriyle ("Attack Lunge") yuvarlanır,
        // sonra "Attack Cooldown" ile ayağa kalkar ve saldırısına baştan başlar.
        private const float MosskinStandTime = 0.25f;
        private IEnumerator RollSpin(GameObject e, HeroController hero)
        {
            yield return null;   // önce genel kesme (davranışı başa sarma + geri itme) uygulansın
            if (e == null) yield break;
            tk2dSpriteAnimator an = e.GetComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimationClip roll = an != null ? an.GetClipByName("Attack Lunge") : null;
            tk2dSpriteAnimationClip up = an != null ? an.GetClipByName("Attack Cooldown") : null;
            if (roll == null) {  yield break; }
            PlayMakerFSM f = FsmByName(e, "Zombie Swipe");
            Behaviour walker = e.GetComponent("Walker") as Behaviour;
            PauseFsm(f, true);
            if (walker != null) walker.enabled = false;
            // Knight'tan uzağa baksın: yuvarlanma geriye doğru gidiyormuş gibi değil, kaçarcasına öne doğru görünsün
            Vector3 sc0 = e.transform.localScale;
            bool heroRight = hero != null && hero.transform.position.x > e.transform.position.x;
            Vector3 sc = sc0; sc.x = Mathf.Abs(sc0.x) * (heroRight ? 1f : -1f); e.transform.localScale = sc;
            an.Play(roll);
            yield return new WaitForSeconds(roll.frames.Length / Mathf.Max(1f, roll.fps));
            if (e == null) yield break;
            if (up != null) { an.Play(up); yield return new WaitForSeconds(up.frames.Length / Mathf.Max(1f, up.fps)); }
            if (e == null) yield break;
            e.transform.localScale = sc0;
            // Ayakta biraz bekler, sonra saldırısına döner
            if (an.GetClipByName("Idle") != null) an.Play("Idle");
            yield return new WaitForSeconds(MosskinStandTime);
            if (e == null) yield break;
            if (walker != null) walker.enabled = true;
            PauseFsm(f, false);
        }

        // Baldur: yuvarlanırken ters yöne döner, kısa süre sonra açılır.
        private const float RollerReverseTime = 0.5f;
        private IEnumerator RollerReverse(GameObject e)
        {
            PlayMakerFSM f = FsmByName(e, "Roller");
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (f == null) yield break;
            string st = f.ActiveStateName;
            bool right = st == "Roll R" || st == "Recoil Decel R";
            bool left = st == "Roll L" || st == "Recoil Decel L";
            if (!right && !left) {  yield break; }
            f.SetState(right ? "Roll L" : "Roll R");
            if (rb != null) rb.velocity = new Vector2((right ? -1f : 1f) * Mathf.Max(4f, Mathf.Abs(rb.velocity.x)), rb.velocity.y);
            yield return new WaitForSeconds(RollerReverseTime);
            if (f != null && (f.ActiveStateName == "Roll L" || f.ActiveStateName == "Roll R")) f.SendEvent("STOP");
        }

        // Squit: atılırken parry'lenince geldiği yöne geri gider; bir yere çarparsa kendi saplanmasıyla saplanır.
        private void SquitReflect(GameObject e)
        {
            PlayMakerFSM f = FsmByName(e, "Mozzie") ?? FsmByName(e, "Mozzie2");
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
            if (f == null || rb == null) return;
            if (f.ActiveStateName != "Lunging") {  return; }
            rb.velocity = -rb.velocity;
            Vector3 sc = e.transform.localScale; sc.x = -sc.x; e.transform.localScale = sc;   // çizim de ters yöne baksın
        }

        // ---------- Flukemarm yaratıkları ----------
        private IEnumerator FlukeFlyBounce(GameObject fly, HeroController hero)
        {
            Rigidbody2D rb = fly.GetComponent<Rigidbody2D>();
            if (rb == null) {  yield break; }
            Vector2 dir = ((Vector2)(fly.transform.position - hero.transform.position)).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            // Bir yere çarpmış gibi: anında ters yöne hızla seker, yavaşlayarak durur, sonra yine kovalar.
            PlayMakerFSM chase = null;
            foreach (var f in fly.GetComponents<PlayMakerFSM>()) if (f.FsmName == "Fluke Fly") chase = f;
            string st0 = chase != null ? chase.ActiveStateName : "?";
            PauseFsm(chase, true);
            float t0 = Time.time;
            const float T = 0.4f, V = 20f;
            while (fly != null && rb != null && Time.time - t0 < T)
            {
                float k = 1f - (Time.time - t0) / T;
                rb.velocity = dir * V * k * k;
                yield return null;
            }
            PauseFsm(chase, false);
        }

        // ---------- Hive Knight ----------
        private static readonly string[] HiveDashRules = { "hive knight|Dash Antic", "hive knight|Dash" };
        private static readonly string[] HiveSlashRules = { "hive knight|Slash 1", "hive knight|Slash 2" };
        private const float HiveBackSpeed = 16f;
        private const float HiveBackTime = 0.3f;

        private IEnumerator HiveDashCancel(GameObject hk, string from)
        {
            if (SkipCancelForStun(hk, "Hive Knight geri gitme")) yield break;
            PlayMakerFSM m = MainFsm(hk);
            Rigidbody2D rb = hk.GetComponent<Rigidbody2D>();
            if (m == null || rb == null) yield break;
            HeroController h = HeroController.instance;
            float dir = h != null && h.transform.position.x > hk.transform.position.x ? -1f : 1f;   // Knight'tan uzağa
            bool slash = from.StartsWith("Slash");
            // Saldırının vuruş alanları kapansın (kaçarken vurmasın).
            foreach (string hn in slash ? new[] { "Slash 1", "Slash 2" } : new[] { "Stab Hit" })
            {
                Transform t = hk.transform.Find(hn);
                if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
            }
            // Saldırı biter, oyunun kendi toparlanma adımına geçer; o sırada geriye kayar.
            string rec = slash ? "Slash Recover" : "Dash Recover";
            if (HasState(m, rec)) m.SetState(rec);
            float t0 = Time.time;
            while (hk != null && rb != null && Time.time - t0 < HiveBackTime)
            {
                float k = 1f - 0.7f * (Time.time - t0) / HiveBackTime;
                rb.velocity = new Vector2(dir * HiveBackSpeed * k, rb.velocity.y);
                yield return null;
            }
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        // ---------- Broken Vessel / Lost Kin ----------
        private static readonly string[] IKDashRules =
        {
            "infected knight|Dash Antic", "infected knight|Dash Attack", "infected knight|Dash Antic 2", "infected knight|Dash Attack 2",
            "infected knight|Dash Antic 3", "infected knight|Dash Attack 3",
            "lost kin|Dash Antic", "lost kin|Dash Attack", "lost kin|Dash Antic 2", "lost kin|Dash Attack 2",
            "lost kin|Dash Antic 3", "lost kin|Dash Attack 3"
        };
        private static readonly string[] IKDstabRules =
        {
            "infected knight|Dstab Antic", "infected knight|Dstab Fall", "infected knight|Dstab Land",
            "lost kin|Dstab Antic", "lost kin|Dstab Fall", "lost kin|Dstab Land"
        };
        private const float IKStunTime = 0.7f;
        private const float IKDstabInvuln = 0.3f;   // dalış parry'sinden sonra, savrulma bitince ek dokunulmazlık

        private void IKDashStun(GameObject boss, string from)
        {
            try
            {
                PlayMakerFSM m = MainFsm(boss);
                Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
                if (m == null) return;
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);   // önünde dursun, arkana geçmesin
                // Dash'in vuruş alanı kapansın.
                Transform hit = boss.transform.Find("Dash Slash Hit");
                if (hit != null && hit.gameObject.activeSelf) hit.gameObject.SetActive(false);
                if (!BossIsStunned(boss)) m.SendEvent("STUN");
                float stunTime = boss.name.ToLowerInvariant().Contains("lost kin") ? IKStunTime * LostKinStunMul : IKStunTime;
                MarkParryStun(boss);   // sersemleme kuralı: iptal yok, sersemleme boyunca hasar x1.5
                GameManager.instance.StartCoroutine(IKStunRoutine(boss, m, rb, stunTime));
            }
            catch (System.Exception e) { Log("HATA (Broken Vessel sersemleme): " + e.Message); }
        }

        private const float LostKinStunMul = 1.3f;   // Lost Kin'in dash sersemlemesi Broken Vessel'inkinden %30 uzun

        private IEnumerator IKStunRoutine(GameObject boss, PlayMakerFSM m, Rigidbody2D rb, float stunTime)
        {
            float t0 = Time.time;
            // İlk an: hızı sıfırda tut (dash hızı bir kare daha uygulanıp arkana geçmesin).
            while (boss != null && rb != null && Time.time - t0 < 0.1f) { rb.velocity = new Vector2(0f, rb.velocity.y); yield return null; }
            while (boss != null && m != null && Time.time - t0 < stunTime) yield return null;
            if (boss == null || m == null) yield break;
            string s = m.ActiveStateName;
            if (s.Contains("Stun") && s != "Stun Recover" && HasState(m, "Stun Recover"))
            {
                m.SetState("Stun Recover");
            }

        }

        // ---------- Yukarıdan aşağı saldırılar (baktığın yönün tersine savrulma) ----------
        private static readonly string[] TopDownRules =
        {
            "infected knight|Dstab Antic", "infected knight|Dstab Fall", "infected knight|Dstab Land",
            "lost kin|Dstab Antic", "lost kin|Dstab Fall", "lost kin|Dstab Land",
            "mantis lord|Dstab Arrive", "mantis lord|Dstab Attack", "mantis lord|Dstab Land",
            "mage knight|Stomp Antic", "mage knight|Stomp Air", "mage knight|Stomp Slash",
            "grimm|AD Fire", "grimm|AD Edge",
            "hk prime|Stomp Antic", "hk prime|Stomp Down",
            "hollow knight boss|Stomp Antic", "hollow knight boss|Stomp Down",
            "traitor lord|DSlash Antic", "traitor lord|DSlash", "traitor lord|Land",
            "mimic spider|Roof Drop", "mimic spider|Falling"
        };
        private static readonly string[] TopDownNames = { "spawn jar", "vomit glob nosk" };

        // ---------- Sadece havadayken parry'lenebilen saldırılar ----------
        // Winged Nosk'un üzerine dalışı: yerdeyken parry'lenemez, zıplayıp havada parry'lersen pogo gibi sıçrarsın.
        private static readonly string[] AirOnlyParryRules =
        {
            "hornet nosk|Swoop Antic", "hornet nosk|Swoop L", "hornet nosk|Swoop R", "hornet nosk|Swoop Rise"
        };
        private string AirOnlyLockedState(GameObject go, HeroController hero)
        {
            if (hero == null || !hero.cState.onGround) return null;
            GameObject body = BodyKey(go);
            if (body == null) return null;
            string st = GetControlState(go);
            return MatchRule(AirOnlyParryRules, body.name, st) ? st + " (sadece havada parry'lenir)" : null;
        }

        // ---------- Uumuu: denizanası topu ----------
        private const float JellyBallSpeed = 14f;      // geri sekme hızı (en az)
        private const float JellyBallAccel = 25f;        // geri giderken saniyede bu kadar hızlanır
        private const float JellyBallMaxSpeedMul = 2.5f; // en fazla başlangıç hızının bu katına çıkar
        private const int JellyBallDamage = 70;        // patladığı yerdeki düşmanlara
        private const float JellyBlastRadius = 2.5f;   // patlamanın etki alanı
        private readonly System.Collections.Generic.HashSet<GameObject> _jellyBalls = new System.Collections.Generic.HashSet<GameObject>();

        private IEnumerator JellyBallReturn(GameObject ball, HeroController hero)
        {
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            // Kovalamayı bırakır ve geri dönerken sana hasar vermez.
            var fsms = ball.GetComponentsInChildren<PlayMakerFSM>();
            foreach (var f in fsms) PauseFsm(f, true);
            foreach (DamageHero dh in ball.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
            Vector2 v0 = rb != null ? rb.velocity : Vector2.zero;
            Vector2 dir = v0.sqrMagnitude > 0.25f ? -v0.normalized
                        : ((Vector2)(ball.transform.position - hero.transform.position)).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = hero.cState.facingRight ? Vector2.right : Vector2.left;
            // Sana gelirken ulaştığı hızla geri gider (kovalarken hızlanıyor); çok yavaşsa en az JellyBallSpeed.
            float speed = Mathf.Max(v0.magnitude, JellyBallSpeed);   // geldiği hızla başlar, sonra gittikçe hızlanır
            float maxSpeed = speed * JellyBallMaxSpeedMul;
            // Yüzü gittiği yöne dönsün: oyun topu hızının yönüne çeviriyor, biz de yön değişimi kadar döndürüyoruz.
            if (v0.sqrMagnitude > 0.25f)
            {
                float turn = Vector2.SignedAngle(v0, dir);
                ball.transform.rotation = Quaternion.Euler(0f, 0f, ball.transform.eulerAngles.z + turn);
            }
            HealthManager selfHm = ball.GetComponent<HealthManager>();
            float t0 = Time.time;
            string why = "süre doldu";
            yield return null;
            while (ball != null && ball.activeInHierarchy && Time.time - t0 < 3f)
            {
                Vector2 p = ball.transform.position;
                speed = Mathf.Min(maxSpeed, speed + JellyBallAccel * Time.deltaTime);   // gittikçe hızlanır
                Vector2 step = dir * speed * Time.deltaTime;
                if (rb != null) rb.velocity = dir * speed; else ball.transform.position = p + step;
                // Duvar/zemin (oyunun arazi katmanı 8)
                if (Physics2D.OverlapCircle(p + step, 0.45f, 1 << 8) != null) { why = "duvara çarptı"; break; }
                // Düşman
                bool hitEnemy = false;
                foreach (Collider2D c in Physics2D.OverlapCircleAll(p + step, 0.7f))
                {
                    HealthManager hm = c.GetComponentInParent<HealthManager>();
                    if (hm == null || hm == selfHm || hm.gameObject.name.StartsWith("Lil Jellyfish")) continue;
                    hitEnemy = true; why = hm.gameObject.name + "'a çarptı"; break;
                }
                if (hitEnemy) break;
                yield return null;
            }
            if (ball == null) yield break;
            Vector2 at = ball.transform.position;
            // Patlama yerindeki düşmanlara 70 hasar (her düşmana bir kez).
            var done = new System.Collections.Generic.HashSet<HealthManager>();
            foreach (Collider2D c in Physics2D.OverlapCircleAll(at, JellyBlastRadius))
            {
                HealthManager hm = c.GetComponentInParent<HealthManager>();
                if (hm == null || hm == selfHm || hm.hp <= 0 || !done.Add(hm)) continue;
                // Uumuu'ya bizim 70 hasarımız işlemez: ona topun kendi patlaması oyundaki gibi etki eder (kabuğu açılır).
                if (hm.gameObject.name.ToLowerInvariant().Contains("mega jellyfish")) {  continue; }
                try
                {
                    hm.Hit(new HitInstance
                    {
                        Source = ball, AttackType = AttackTypes.Generic, DamageDealt = JellyBallDamage,
                        Direction = dir.x >= 0 ? 0f : 180f, Multiplier = 1f, MagnitudeMultiplier = 1f, IgnoreInvulnerable = true
                    });
                }
                catch (System.Exception e) { Log("HATA (denizanası patlaması): " + e.Message); }
            }
            // Topun kendi patlaması: oyunda patladığında ne oluyorsa aynısı (efekt, ses, Uumuu'nun kabuğunun açılması).
            // Oyunda top "Die" adımında patlar (Hall of Gods'da "Uumuu Explosion").
            UnguardJelly(ball);
            bool native = false;
            foreach (var f in fsms)
            {
                if (f == null || f.FsmName != "Lil Jelly") continue;
                f.enabled = true;
                if (HasState(f, "Die")) { f.SetState("Die"); native = true; }
            }
            if (!native)
            {
                if (selfHm != null && selfHm.hp > 0) selfHm.Die(dir.x >= 0 ? 0f : 180f, AttackTypes.Generic, true);
                else ball.SetActive(false);
            }
            _jellyBalls.Remove(ball);
        }

        // Denizanası topu Knight'a değdiği an kendiliğinden patlıyor (oyunda değince "ZERO HP" olup "Die"a geçer),
        // bu yüzden parry'den ÖNCE patlıyordu. Parry penceresi açılınca yakındaki topların bu patlaması geçici olarak
        // kapatılır ("ZERO HP" -> kovalamaya devam). Pencere bitince (parry'lenmediyse) eski haline döner.
        private readonly System.Collections.Generic.Dictionary<PlayMakerFSM, System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>> _jellyGuards =
            new System.Collections.Generic.Dictionary<PlayMakerFSM, System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>>();

        private void GuardJellies(HeroController hero)
        {
            try
            {
                foreach (PlayMakerFSM f in UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>())
                {
                    if (f == null || f.FsmName != "Lil Jelly" || _jellyGuards.ContainsKey(f)) continue;
                    if (Vector2.Distance(f.transform.position, hero.transform.position) > 6f) continue;
                    HutongGames.PlayMaker.FsmState chase = null;
                    foreach (var st in f.FsmStates) if (st.Name == "Chase") chase = st;
                    if (chase == null || f.FsmGlobalTransitions == null) continue;
                    foreach (var tr in f.FsmGlobalTransitions)
                    {
                        if (tr.EventName != "ZERO HP") continue;
                        _jellyGuards[f] = new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmTransition, HutongGames.PlayMaker.FsmState>(tr, tr.ToFsmState);
                        tr.ToState = "Chase"; tr.ToFsmState = chase;
                        GameManager.instance.StartCoroutine(UnguardJellyLater(f.gameObject, f));
                        break;
                    }
                }
            }
            catch (System.Exception e) { Log("HATA (denizanası koruması): " + e.Message); }
        }

        private IEnumerator UnguardJellyLater(GameObject ball, PlayMakerFSM f)
        {
            float until = _windowEnd + 0.1f;
            while (Time.time < until) yield return null;
            if (ball != null && _jellyBalls.Contains(ball)) yield break;   // parry'lendi: dönüşü bitince patlar
            UnguardJelly(ball);
            // Pencere sırasında canı bittiyse (iğneyle vurulduysa) şimdi patlasın.
            HealthManager hm = ball != null ? ball.GetComponent<HealthManager>() : null;
            if (f != null && hm != null && hm.hp <= 0 && HasState(f, "Die")) f.SetState("Die");
        }

        private void UnguardJelly(GameObject ball)
        {
            if (ball == null) return;
            foreach (PlayMakerFSM f in ball.GetComponents<PlayMakerFSM>())
            {
                if (f == null || !_jellyGuards.TryGetValue(f, out var kv)) continue;
                kv.Key.ToState = kv.Value != null ? kv.Value.Name : "Die";
                kv.Key.ToFsmState = kv.Value;
                _jellyGuards.Remove(f);
            }
        }

        // ---------- False Knight / Failed Champion düşen topları ----------
        // Topun kendi kırılma adımı varsa o oynar (efekt ve sesiyle); yoksa yok olur.
        private void BreakBarrel(GameObject barrel)
        {
            try
            {
                HealthManager hm = barrel.GetComponentInChildren<HealthManager>();
                if (hm != null && hm.hp > 0)
                {
                    hm.Die(null, AttackTypes.Nail, true);
                    return;
                }
                foreach (PlayMakerFSM f in barrel.GetComponentsInChildren<PlayMakerFSM>())
                {
                    string names = "", pick = null;
                    foreach (var st in f.FsmStates)
                    {
                        names += st.Name + ", ";
                        string l = st.Name.ToLowerInvariant();
                        if (pick == null && (l.Contains("break") || l.Contains("burst") || l.Contains("smash") || l.Contains("destroy") || l.Contains("explode")))
                            pick = st.Name;
                    }
                    if (pick != null) { f.SetState(pick);  return; }
                }
                barrel.SetActive(false);
            }
            catch (System.Exception e) { Log("HATA (düşen top): " + e.Message); }
        }

        // ---------- Collector ----------
        private static readonly string[] CollectorGrabRules = { "jar collector|Lunge Antic", "jar collector|Lunge Air", "jar collector|Lunge Swipe" };

        private const float CollectorStunTime = 0.5f;

        private void CollectorGrabStun(GameObject boss, string from)
        {
            try
            {
                PlayMakerFSM m = MainFsm(boss);
                if (m == null) return;
                Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                Transform hit = boss.transform.Find("Lunge Hit");
                if (hit != null && hit.gameObject.activeSelf) hit.gameObject.SetActive(false);
                if (!BossIsStunned(boss)) m.SendEvent("STUN");
                MarkParryStun(boss);   // sersemleme kuralı: sersemleme boyunca hasar x1.5
                GameManager.instance.StartCoroutine(CollectorStunRoutine(boss, m));
            }
            catch (System.Exception e) { Log("HATA (Collector sersemleme): " + e.Message); }
        }

        private IEnumerator CollectorStunRoutine(GameObject boss, PlayMakerFSM m)
        {
            yield return new WaitForSeconds(CollectorStunTime);
            if (boss == null || m == null) yield break;
            string s = m.ActiveStateName;
            if (s.Contains("Stun") && s != "Stun Recover" && HasState(m, "Stun Recover"))
            {
                m.SetState("Stun Recover");
            }

        }

        // Parry'lenince yok olan enfeksiyon mermileri (adın parçası)
        private static readonly string[] InfectionShotNames = { "vomit glob", "shot mawlek", "ik projectile", "shot hk glob" };

        // ---------- God Tamer'ın büyük yaratığı (Lobster) ----------
        private static readonly string[] LancerSwordRules = { "lancer|Slash 1", "lancer|Slash 2", "lancer|In Air" };
        private static readonly string[] LobsterRollRules = { "lobster|RC Charge", "lobster|RC Charging" };
        private static readonly string[] LobsterLandRules = { "lobster|RC Air", "lobster|RC Land" };
        private const float LobsterBackSpeed = 14f;
        private const float LobsterBackTime = 0.3f;

        private IEnumerator LobsterRollCancel(GameObject lob, string from)
        {
            if (SkipCancelForStun(lob, "God Tamer yaratığı geri dönme")) yield break;
            PlayMakerFSM m = MainFsm(lob);
            Rigidbody2D rb = lob.GetComponent<Rigidbody2D>();
            if (m == null || rb == null) yield break;
            float dir = Mathf.Abs(rb.velocity.x) > 1f ? -Mathf.Sign(rb.velocity.x)
                      : (HeroController.instance != null && HeroController.instance.transform.position.x > lob.transform.position.x ? -1f : 1f);
            // 1) Dönerek biraz geri gider (yavaşlayarak). Duvara değip sekmesin diye o sırada durum makinesi bekler.
            PauseFsm(m, true);
            float t0 = Time.time;
            while (lob != null && rb != null && Time.time - t0 < LobsterBackTime)
            {
                float k = 1f - 0.7f * (Time.time - t0) / LobsterBackTime;
                rb.velocity = new Vector2(dir * LobsterBackSpeed * k, rb.velocity.y);
                yield return null;
            }
            // 2) Dönmeyi bitirip toparlanır. Oyunun iniş adımı ("RC Land") düşme sesi ve animasyonu oynattığı için
            // kullanılmıyor: dönmeye başlarken ("RC Antic") açılan/kapanan parçalar (dönme vuruşu, iz) eski haline getirilir,
            // dönme sesi durur, dokunulmazlık kalkar ve normal bekleme adımına ("Idle") geçer.
            if (lob == null || m == null) yield break;
            UndoRollParts(m);
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            foreach (AudioSource a in lob.GetComponentsInChildren<AudioSource>()) if (a != null && a.isPlaying) a.Stop();
            HealthManager lhm = lob.GetComponent<HealthManager>();
            if (lhm != null) lhm.IsInvincible = false;
            // Toparlanma animasyonu: iniş adımının ("RC Land") animasyonu oynar ama adımın kendisi çalıştırılmaz,
            // böylece yukarıdan düşme sesi çıkmaz. Animasyon bitince normal bekleme adımına geçer.
            string landClip = GetStateClip(m, "RC Land");
            tk2dSpriteAnimator lan = lob.GetComponent<tk2dSpriteAnimator>();
            if (landClip != null && lan != null)
            {
                tk2dSpriteAnimationClip c = lan.GetClipByName(landClip);
                if (c != null)
                {
                    lan.Play(c);
                    float dur = c.fps > 0f && c.frames != null ? c.frames.Length / c.fps : 0.4f;
                    float tA = Time.time + Mathf.Min(dur, 1.5f);
                    while (lob != null && Time.time < tA) { if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y); yield return null; }
                    if (lob == null || m == null) yield break;
                }
            }

            PauseFsm(m, false);
            if (HasState(m, "Idle")) m.SetState("Idle");
        }

        // "RC Antic" adımındaki parça açma/kapama eylemlerini tersine çevirir.
        private void UndoRollParts(PlayMakerFSM m)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var st in m.FsmStates)
            {
                if (st.Name != "RC Antic" || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || a.GetType().Name != "ActivateGameObject") continue;
                    try
                    {
                        var od = a.GetType().GetField("gameObject")?.GetValue(a) as HutongGames.PlayMaker.FsmOwnerDefault;
                        var act = a.GetType().GetField("activate")?.GetValue(a) as HutongGames.PlayMaker.FsmBool;
                        GameObject t = od != null ? m.Fsm.GetOwnerDefaultTarget(od) : null;
                        if (t == null || t == m.gameObject || act == null) continue;
                        t.SetActive(!act.Value);
                        names.Add(t.name + (act.Value ? " kapandı" : " açıldı"));
                    }
                    catch (System.Exception e) { names.Add("hata: " + e.Message); }
                }
            }
        }

        // ---------- Gruz Mother ----------
        private static readonly string[] GruzChargeRules = { "giant fly|Charge Antic", "giant fly|Charge" };
        private const float GruzBackSpeed = 16f;   // geriye gitme hızı
        private const float GruzBackTime = 0.35f;  // geriye gitme süresi

        private IEnumerator GruzChargeCancel(GameObject fly)
        {
            if (SkipCancelForStun(fly, "Gruz Mother geri gitme")) yield break;
            PlayMakerFSM m = MainFsm(fly);
            Rigidbody2D rb = fly.GetComponent<Rigidbody2D>();
            if (m == null || rb == null) yield break;
            string from = m.ActiveStateName;
            // Geldiği yönün tersine (hız yoksa Knight'tan uzağa).
            Vector2 dir;
            if (rb.velocity.sqrMagnitude > 1f) dir = -rb.velocity.normalized;
            else
            {
                Vector3 hp = HeroController.instance != null ? HeroController.instance.transform.position : fly.transform.position;
                dir = ((Vector2)(fly.transform.position - hp)).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            }
            // Saldırı kesilsin: normal uçuşuna dönsün. ("Flying" çarpma saldırısının adımı: oraya gitmesin,
            // yoksa çarpma saldırısının uyarısı çıkıyor ve gövdesi parry'lenemez oluyordu.)
            if (HasState(m, "Super End")) m.SetState("Super End");
            else if (HasState(m, "Buzz")) m.SetState("Buzz");
            float t0 = Time.time;
            while (fly != null && rb != null && Time.time - t0 < GruzBackTime)
            {
                float k = 1f - 0.7f * (Time.time - t0) / GruzBackTime;   // yavaşlayarak
                rb.velocity = dir * GruzBackSpeed * k;
                yield return new WaitForFixedUpdate();
            }
            if (rb != null) rb.velocity = Vector2.zero;
        }

        // ---------- Watcher Knight ----------
        private static readonly string[] WatcherRollRules =
        {
            "black knight|Charge Antic", "black knight|Charge", "black knight|Hit Wall", "black knight|Scale Sparks"
        };
        private const float WatcherBackSpeed = 14f;  // top halinde geri yuvarlanma hızı
        private const float WatcherBackTime = 0.25f; // top halinde geri yuvarlanma süresi

        private IEnumerator WatcherRollCancel(GameObject knight)
        {
            if (SkipCancelForStun(knight, "Watcher Knight geri yuvarlanma")) yield break;
            PlayMakerFSM m = MainFsm(knight);
            Rigidbody2D rb = knight.GetComponent<Rigidbody2D>();
            if (m == null) yield break;
            string from = m.ActiveStateName;
            // Geldiği yönün tersine gitsin (hız yoksa Knight'tan uzağa).
            float dir;
            if (rb != null && Mathf.Abs(rb.velocity.x) > 1f) dir = -Mathf.Sign(rb.velocity.x);
            else dir = HeroController.instance != null && HeroController.instance.transform.position.x > knight.transform.position.x ? -1f : 1f;
            // Hazırlık anındaysa önce top haline geçsin.
            if ((from == "Charge Antic" || from == "Scale Sparks") && HasState(m, "Charge")) m.SetState("Charge");

            // 1) Top halinde geriye yuvarlan (yavaşlayarak). Oyun yuvarlanma hızını her fizik adımında
            // "Charge Speed Crt" değişkeninden yeniden verdiği için o değişkeni de ters yöne çeviriyoruz.
            HutongGames.PlayMaker.FsmFloat crt = m.FsmVariables.FindFsmFloat("Charge Speed Crt");
            float t0 = Time.time;
            while (knight != null && rb != null && Time.time - t0 < WatcherBackTime)
            {
                float k = 1f - 0.6f * (Time.time - t0) / WatcherBackTime;
                float v = dir * WatcherBackSpeed * k;
                if (crt != null) crt.Value = v;
                rb.velocity = new Vector2(v, rb.velocity.y);
                yield return null;
            }
            // 2) Sonra top formundan çık; toparlanıp yeni saldırısını seçer.
            if (knight != null && m != null)
            {
                string st = m.ActiveStateName;
                if ((st == "Charge" || st == "Hit Wall" || st == "Charge Antic" || st == "Scale Sparks") && HasState(m, "Charge End"))
                    m.SetState("Charge End");
            }
        }

        // ---------- Xero kılıçları ----------
        private bool _xeroStatesLogged;
        private static readonly string[] XeroReturnCandidates = { "Return", "Return Antic", "Return Start", "Recover", "Recall" };
        private void XeroSwordReturn(GameObject sword)
        {
            try
            {
                Rigidbody2D rb = sword.GetComponent<Rigidbody2D>();
                Vector2 vBefore = rb != null ? rb.velocity : Vector2.zero;
                string done = null;
                foreach (PlayMakerFSM f in sword.GetComponents<PlayMakerFSM>())
                {
                    if (!_xeroStatesLogged)
                    {
                        var names = new System.Collections.Generic.List<string>();
                        foreach (var st in f.FsmStates) names.Add(st.Name);
                    }
                    if (done != null) continue;
                    string target = null;
                    foreach (string c in XeroReturnCandidates) if (HasState(f, c)) { target = c; break; }
                    if (target == null)
                        foreach (var st in f.FsmStates)
                            if (st.Name.ToLowerInvariant().Contains("return")) { target = st.Name; break; }
                    if (target != null && f.ActiveStateName != target)
                    {
                        string from = f.ActiveStateName;
                        f.SetState(target);
                        done = f.FsmName + ": '" + from + "' -> '" + target + "'";
                    }
                }
                _xeroStatesLogged = true;
                // Dönüş durumu bulunamadıysa en azından geldiği yöne geri fırlat.
                if (done == null && rb != null)
                {
                    rb.velocity = -vBefore;
                    done = "dönüş durumu yok, hız ters çevrildi " + vBefore + " -> " + rb.velocity;
                }
            }
            catch (System.Exception) {  }
        }

        // ---------- Galien çekici ve Markoth kalkanı ----------
        private const float HammerBounceTime = 0.7f;   // çekicin yukarı sekme süresi
        private const float HammerBounceUp = 22f;      // yukarı fırlama hızı
        private const float HammerBounceSide = 7f;     // senden uzağa yatay hız
        private const float HammerBounceGravity = 35f;

        private static GameObject FindAncestor(GameObject go, string namePart)
        {
            for (Transform t = go != null ? go.transform : null; t != null; t = t.parent)
                if (t.name.ToLowerInvariant().Contains(namePart) || Canon(t.name).Contains(namePart)) return t.gameObject;
            return null;
        }

        private readonly System.Collections.Generic.HashSet<GameObject> _bouncing = new System.Collections.Generic.HashSet<GameObject>();
        private IEnumerator HammerBounce(GameObject hammer, HeroController hero)
        {
            if (!_bouncing.Add(hammer)) yield break;
            // Çekiç için ayrı ses yok (ileride genel parry sesi eklenecek).
            Rigidbody2D rb = hammer.GetComponent<Rigidbody2D>();
            bool useRb = rb != null && rb.bodyType == RigidbodyType2D.Dynamic;
            float side = hammer.transform.position.x >= hero.transform.position.x ? 1f : -1f;
            Vector2 v = new Vector2(side * HammerBounceSide, HammerBounceUp);
            var fsms = new System.Collections.Generic.List<string>();
            foreach (PlayMakerFSM f in hammer.GetComponents<PlayMakerFSM>()) fsms.Add(f.FsmName + "='" + f.ActiveStateName + "'");
            float t0 = Time.time;
            while (hammer != null && Time.time - t0 < HammerBounceTime)
            {
                float dt = Time.deltaTime;
                v.y -= HammerBounceGravity * dt;
                if (useRb) rb.velocity = v;
                else hammer.transform.position += (Vector3)(v * dt);
                yield return null;
            }
            _bouncing.Remove(hammer);
        }

        // Kalkanın dönüşünü tersine çevirir: oyun her karede kalkanı ne kadar döndürdüyse biz onu
        // ters yöne uygularız (dönüş nasıl yapılıyor olursa olsun çalışır). Tekrar parry'lersen yine döner.
        private readonly System.Collections.Generic.Dictionary<Transform, bool> _shieldReversed = new System.Collections.Generic.Dictionary<Transform, bool>();
        private float _lastShieldFlip;
        private void ReverseMarkothShields(GameObject hit)
        {
            if (Time.time - _lastShieldFlip < 0.4f) return; // aynı parry'de iki kalkan birden değerse bir kez say
            _lastShieldFlip = Time.time;
            var roots = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
                if (t.name.StartsWith("Markoth Shield")) roots.Add(t);
            GameObject own = FindAncestor(hit, "markoth shield");
            if (own != null && !roots.Contains(own.transform)) roots.Add(own.transform);
            foreach (Transform root in roots)
            {
                bool now = !(_shieldReversed.TryGetValue(root, out bool rev) && rev);
                bool isNew = !_shieldReversed.ContainsKey(root);
                _shieldReversed[root] = now;
                if (isNew) GameManager.instance.StartCoroutine(ShieldMirror(root));
            }
        }

        private IEnumerator ShieldMirror(Transform root)
        {
            // Kökü ve kalkan parçalarını izle (hangisi dönüyorsa o ters çevrilir).
            var parts = new System.Collections.Generic.List<Transform> { root };
            foreach (Transform c in root) parts.Add(c);
            var lastSet = new System.Collections.Generic.Dictionary<Transform, float>();
            foreach (Transform p in parts) lastSet[p] = p.localEulerAngles.z;
            float logAt = Time.time + 1f;
            while (root != null)
            {
                bool rev = _shieldReversed.TryGetValue(root, out bool r2) && r2;
                foreach (Transform p in parts)
                {
                    if (p == null) continue;
                    float cur = p.localEulerAngles.z;
                    float delta = Mathf.DeltaAngle(lastSet[p], cur); // oyunun bu karedeki dönüşü
                    float next = rev ? lastSet[p] - delta : cur;
                    if (rev) p.localEulerAngles = new Vector3(p.localEulerAngles.x, p.localEulerAngles.y, next);
                    lastSet[p] = next;
                    if (rev && Time.time > logAt && Mathf.Abs(delta) > 0.01f)
                    {
                        logAt = float.MaxValue;
                    }
                }
                yield return null;
            }
            _shieldReversed.Remove(root);
        }

        // ---------- Sersemleme patlama efekti ----------
        private const bool StunBurstEnabled = true;
        private const int StunBurstParticles = 16;   // etrafa saçılan beyaz parçacık sayısı
        private static Sprite _discSprite;

        // Knight ile bossun gövdesi iç içeyse, ayrılmaları için bossun yatayda ne kadar kayması gerektiği (0 = değmiyorlar).
        private static float OverlapPushNeeded(GameObject boss)
        {
            try
            {
                HeroController hc = HeroController.instance;
                if (hc == null || boss == null) return 0f;
                Collider2D hcol = hc.GetComponent<Collider2D>();
                Collider2D bcol = boss.GetComponent<Collider2D>();
                if (hcol == null || bcol == null) return 0f;
                Bounds a = hcol.bounds, b = bcol.bounds;
                if (!a.Intersects(b)) return 0f;
                return (a.extents.x + b.extents.x) - Mathf.Abs(a.center.x - b.center.x);
            }
            catch (System.Exception) { return 0f; }
        }

        private static Vector3 BossCenter(GameObject boss)
        {
            Renderer rd = boss.GetComponent<Renderer>();
            return rd != null ? rd.bounds.center : boss.transform.position;
        }

        private IEnumerator StunBurst(Vector3 pos)
        {
            pos.z = -0.5f;
            var objs = new System.Collections.Generic.List<GameObject>();
            System.Func<string, Sprite, SpriteRenderer> make = (name, spr) =>
            {
                GameObject g = new GameObject(name);
                g.transform.position = pos;
                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingLayerName = "Effects";
                sr.sortingOrder = 1001;
                objs.Add(g);
                return sr;
            };

            SpriteRenderer flash = make("StunFlash", GetDiscSprite());
            SpriteRenderer ring1 = make("StunRing1", GetRingSprite());
            var parts = new System.Collections.Generic.List<SpriteRenderer>();
            var vels = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < StunBurstParticles; i++)
            {
                SpriteRenderer pr = make("StunSpark", GetDiscSprite());
                pr.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.12f, 0.28f);
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                vels.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f + 0.3f) * UnityEngine.Random.Range(7f, 15f));
                parts.Add(pr);
            }

            const float dur = 0.7f;
            float t = 0f;
            while (t < dur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float k = Mathf.Clamp01(t / dur);

                // Parlama: hızlı büyüyüp söner.
                float fk = Mathf.Clamp01(t / 0.3f);
                flash.transform.localScale = Vector3.one * Mathf.Lerp(1f, 5.5f, Mathf.Sqrt(fk));
                flash.color = new Color(1f, 1f, 1f, 0.85f * (1f - fk));
                // Halka (eskiden gecikmeli ikinci bir halka da vardı: efekt iki kez çıkıyor gibi görünüyordu).
                ring1.transform.localScale = Vector3.one * Mathf.Lerp(1f, 8f, Mathf.Sqrt(k));
                ring1.color = new Color(1f, 1f, 1f, 1f - k);
                // Parçacıklar: dışa saçılır, yavaşlar, hafif düşer, söner.
                for (int i = 0; i < parts.Count; i++)
                {
                    vels[i] = vels[i] * (1f - 3f * dt) + new Vector2(0f, -12f * dt);
                    parts[i].transform.position += (Vector3)(vels[i] * dt);
                    parts[i].color = new Color(1f, 1f, 1f, 1f - k);
                }
                yield return null;
            }
            foreach (GameObject g in objs) if (g != null) UnityEngine.Object.Destroy(g);
        }

        // Ortası parlak, kenarı yumuşak beyaz daire.
        private static Sprite GetDiscSprite()
        {
            if (_discSprite != null) return _discSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _discSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return _discSprite;
        }

        // Halka görselini kodla üretir (dışarıdan resim dosyasına gerek yok).
        private static Sprite GetRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 56f) / 6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return _ringSprite;
        }

        // Saldırıyı yapan nesnenin ve üst nesnelerinin isimlerini tek yazıda toplar.
        private static string DescribeAttacker(GameObject go)
        {
            if (go == null) return "(bilinmiyor)";

            StringBuilder sb = new StringBuilder();
            Transform t = go.transform;
            while (t != null)
            {
                if (sb.Length > 0) sb.Append(" <- ");
                sb.Append(t.name);
                if (t.name.StartsWith("[Architect]")) sb.Append(" {").Append(Canon(t.name)).Append("}");   // kurallar iç adla eşleşsin
                t = t.parent;
            }
            return sb.ToString();
        }

        private static bool ContainsAny(string text, string[] parts)
        {
            string lower = text.ToLowerInvariant();
            foreach (string part in parts)
            {
                if (lower.Contains(part)) return true;
            }
            return false;
        }

        // Her sahneye girişte sahnedeki düşmanların gerçek adlarını log'a yazar.
        private void OnSceneChanged(Scene from, Scene to)
        {
            if (GameManager.instance == null) return;
            if (to.name == ShinobiScene) GameManager.instance.StartCoroutine(ShinobiPickupRoutine());
            _seenHazards.Clear(); _stateHist.Clear(); _dangerPrev.Clear();
            _dangerFsms.Clear(); _dangerLastState.Clear(); _fightStartWarned.Clear(); _entryHooked.Clear(); _dangerCooldown.Clear(); _activeHazards.Clear(); _dangerRefreshAt = 0f;
            GameManager.instance.StartCoroutine(LogEnemiesRoutine(to.name));
        }

        private IEnumerator LogEnemiesRoutine(string sceneName)
        {
            yield return new WaitForSeconds(2f); // düşmanların yüklenmesini bekle
            HealthManager[] all = UnityEngine.Object.FindObjectsOfType<HealthManager>();
            _bossControls.Clear();
            foreach (HealthManager hm in all)
            {

                // Bossların durum makinesini kaydet: bossa bağlı olmayan patlama/tendril'ler için gerekli.
                if (IsBoss(hm))
                {
                    foreach (PlayMakerFSM fsm in hm.gameObject.GetComponents<PlayMakerFSM>())
                    {
                        if (System.Array.IndexOf(MainFsmNames, fsm.FsmName) >= 0 && !_bossControls.Contains(fsm))
                            _bossControls.Add(fsm);

                    }
                }
            }
        }


        // Boss parry'lenemez bir saldırının (odaklanma, void tendril) içinde mi?
        // Vuruş bossa bağlıysa o bossa, bağlı değilse (ayrı patlama nesnesi) sahnedeki bosslara bakar.
        private readonly System.Collections.Generic.List<PlayMakerFSM> _bossControls =
            new System.Collections.Generic.List<PlayMakerFSM>();


        private PlayMakerFSM FindBossControlInStates(string[] states)
        {
            foreach (PlayMakerFSM c in _bossControls)
            {
                if (c == null) continue;
                if (System.Array.IndexOf(states, c.ActiveStateName) >= 0) return c;
            }
            return null;
        }

        // Bossa bağlı vuruşlar için ek yasaklar: gövdesi parry'lenemeyen bosslar ve
        // "sadece bossa bağlı olanlar parry'lenemez" kuralları.
        private string AttachedLockedState(GameObject go)
        {
            if (go == null) return null;
            HealthManager hm = go.GetComponentInParent<HealthManager>();
            if (hm == null) return null;
            string name = hm.gameObject.name;
            string lower = name.ToLowerInvariant();

            PlayMakerFSM m = MainFsm(hm.gameObject);
            string st = m != null ? m.ActiveStateName : null;

            // Gövde: vuruşun kaynağı bossun kendisi. Odaklı dash sırasında gövde saldırının kendisi,
            // o yüzden orada parry'lenebilir.
            if (go == hm.gameObject)
            {
                bool dashing = MatchRule(DashStopRules, name, st) || st == "D Slash Recover";
                if (!dashing)
                {
                    foreach (string b in BodyUnparryableBosses)
                        if (lower.Contains(b)) return "gövde";
                }
            }

            if (m != null && MatchRule(UnparryableAttachedRules, name, m.ActiveStateName))
                return m.ActiveStateName + " (gövde)";
            return null;
        }

        // Odaklı dash'i senin hizanda bitirir. Her karede çalışır.
        private float _nextBossRefresh;
        private readonly System.Collections.Generic.HashSet<PlayMakerFSM> _dashSeen =
            new System.Collections.Generic.HashSet<PlayMakerFSM>();
        private readonly System.Collections.Generic.HashSet<PlayMakerFSM> _dashStopped =
            new System.Collections.Generic.HashSet<PlayMakerFSM>();

        private void DashStopTick(HeroController hero)
        {
            try
            {
                // Sonradan sahneye giren bosslar (ör. Mato) için listeyi ara ara yenile.
                if (Time.time >= _nextBossRefresh)
                {
                    _nextBossRefresh = Time.time + 2f;
                    RefreshBossControls();
                }

                float hx = hero.transform.position.x;
                foreach (PlayMakerFSM c in _bossControls)
                {
                    if (c == null || !c.enabled) continue; // sersemlemiş (donmuş) bossa dokunma
                    string s = c.ActiveStateName;
                    if (!MatchRule(DashStopRules, c.gameObject.name, s))
                    {
                        _dashStopped.Remove(c);
                        _dashSeen.Remove(c);
                        continue;
                    }
                    if (_dashStopped.Contains(c)) continue;
                    if (_dashSeen.Add(c))
                    {
                        // Boss sana doğru koşuyor: parry'de bu yöne (bossun gittiği yöne) savrulursun.
                        _dashDirRight[c.gameObject] = hx > c.transform.position.x;
                        _dashDirTime[c.gameObject] = Time.time;
                    }

                    // Yön: durum adından değil, gerçek hareketten (ad yanıltıcı olabilir).
                    Rigidbody2D rb = c.GetComponent<Rigidbody2D>();
                    float vx = rb != null ? rb.velocity.x : 0f;
                    if (Mathf.Abs(vx) < 0.5f) continue; // henüz hızlanmadı
                    bool left = vx < 0f;
                    float bx = c.transform.position.x;
                    bool reached = left ? bx <= hx + DashStopOffset : bx >= hx - DashStopOffset;
                    if (!reached) continue;

                    _dashStopped.Add(c);
                    bool running = s.StartsWith("Dashing");

                    // Hızın kaynağını kapat: durum makinesi hızı her karede bu değişkenden yeniden ayarlıyor
                    // (Sly'da "D Slash Speed"). O anki dash hızına eşit olan değişkenler geçici olarak 0 olur.
                    var speedVars = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>>();
                    StringBuilder names = new StringBuilder();
                    foreach (var f in c.FsmVariables.FloatVariables)
                    {
                        if (f.Name.ToLowerInvariant().Contains("speed")
                            && Mathf.Abs(f.Value) > 5f && Mathf.Abs(Mathf.Abs(f.Value) - Mathf.Abs(vx)) < 1f)
                        {
                            speedVars.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>(f, f.Value));
                            if (names.Length > 0) names.Append(", ");
                            names.Append(f.Name + "=" + f.Value.ToString("0"));
                            f.Value = 0f;
                        }
                    }

                    if (running) c.SendEvent("END"); // koşmayı bitir -> dash sonu kılıcı
                    if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                    GameManager.instance.StartCoroutine(HoldDashStop(c, rb, bx, speedVars));
                }
            }
            catch (System.Exception e) { Log("HATA (dash durdurma): " + e); }
        }

        // Dash durduktan sonra dash sonu kılıcı bitene kadar boss yatayda kaymasın.
        private readonly System.Collections.Generic.Dictionary<GameObject, bool> _dashDirRight = new System.Collections.Generic.Dictionary<GameObject, bool>();
        private readonly System.Collections.Generic.Dictionary<GameObject, float> _dashDirTime = new System.Collections.Generic.Dictionary<GameObject, float>();
        private float _lastHitTime;
        private GameObject _lastHitRoot;
        private const bool DashForceConnect = true;   // durdurulan dash'in kılıcı mutlaka değsin
        private const float DashForceReachX = 4.5f;   // kılıcın yatay menzili
        private const float DashForceReachY = 2.2f;   // bundan yüksekteysen (zıpladıysan) değmez

        // Bossun açık olan saldırı vuruş alanı (gövdesi hariç).
        private static GameObject ActiveAttackHitbox(GameObject boss)
        {
            foreach (DamageHero dh in boss.GetComponentsInChildren<DamageHero>())
                if (dh != null && dh.gameObject != boss && dh.damageDealt > 0) return dh.gameObject;
            return null;
        }

        private static readonly string[] DashEndStates =
        {
            "D Slash S1", "DSlash S2", "Fall",               // Sly
            "D Slash", "D Slash Recover", "D Slash End"      // Oro ve Mato
        };

        private IEnumerator HoldDashStop(PlayMakerFSM c, Rigidbody2D rb, float stopX,
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> speedVars)
        {
            float timeout = Time.time + 1.5f;
            float maxDrift = 0f;
            float stopTime = Time.time;
            float hitboxSeen = -1f;
            bool forced = false;
            yield return null;
            while (c != null && rb != null && Time.time < timeout)
            {
                string s = c.ActiveStateName;
                if (System.Array.IndexOf(DashEndStates, s) < 0 && System.Array.IndexOf(new[] { "Dashing L", "Dashing R" }, s) < 0)
                    break;
                rb.velocity = new Vector2(0f, rb.velocity.y);

                // Dash'i senin önünde durdurduğumuz için kılıç bazen sana tam değmiyor (normalde boss kayarak
                // içinden geçerdi). Kılıç açıkken sen menzildeysen ve hiç vuruş gelmediyse vuruşu biz uygularız:
                // pencere açıksa parry olur, değilse normal hasar alırsın. Zıplayıp üstünden geçtiysen olmaz.
                if (!forced && DashForceConnect)
                {
                    GameObject hb = ActiveAttackHitbox(c.gameObject);
                    if (hb != null && hitboxSeen < 0f) hitboxSeen = Time.time;
                    bool hitSinceStop = _lastHitTime >= stopTime && _lastHitRoot == c.gameObject;
                    if (hitSinceStop) forced = true; // zaten değdi, bir şey yapma
                    else if (hb != null && Time.time - hitboxSeen > 0.06f)
                    {
                        HeroController hc = HeroController.instance;
                        if (hc != null && !hc.cState.invulnerable)
                        {
                            float dx = hc.transform.position.x - c.transform.position.x;
                            float dy = hc.transform.position.y - c.transform.position.y;
                            if (Mathf.Abs(dx) <= DashForceReachX && Mathf.Abs(dy) <= DashForceReachY)
                            {
                                forced = true;
                                DamageHero d = hb.GetComponent<DamageHero>();
                                hc.TakeDamage(hb, dx > 0 ? CollisionSide.left : CollisionSide.right,
                                    d != null && d.damageDealt > 0 ? d.damageDealt : 1, 1);
                            }
                        }
                    }
                }

                // Yedek: yine de kaydıysa durdurulduğu noktaya geri koy.
                float drift = Mathf.Abs(c.transform.position.x - stopX);
                if (drift > maxDrift) maxDrift = drift;
                if (drift > 0.3f)
                {
                    Vector3 pos = c.transform.position;
                    c.transform.position = new Vector3(stopX, pos.y, pos.z);
                    rb.position = new Vector2(stopX, rb.position.y);
                }
                yield return null;
            }

            // Hız değişkenlerini eski haline getir.
            foreach (var kv in speedVars) kv.Key.Value = kv.Value;

        }

        // Boss mu? Canı bir kez 120'nin üstünde görüldüyse, canı sonradan düşse de boss sayılmaya devam eder.
        // (Eskiden can 120'nin altına inince Oro/Mato artık boss sayılmıyordu, sersemleme bu yüzden çalışmıyordu.)
        private readonly System.Collections.Generic.HashSet<HealthManager> _knownBosses = new System.Collections.Generic.HashSet<HealthManager>();
        // Canı yüksek ama boss olmayan normal düşmanlar (Mawlurk, Great Hopper): boss kurallarına girmez,
        // varken sahne "boss savaşı" sayılmaz (yoksa attıkları enfeksiyonlar parry'de yok olmuyordu).
        private readonly System.Collections.Generic.HashSet<HealthManager> _bigNormals = new System.Collections.Generic.HashSet<HealthManager>();
        private static bool IsBigNormalEnemy(GameObject g)
        {
            if (g == null) return false;
            if (FsmByName(g, "Mawlek Turret") != null) return true;   // Mawlurk
            if (dandy0(g) == "gorgeous") return true;                   // Gorgeous Husk
            string gn = Canon(g.name);
            if (FsmByName(g, "ZombieShieldControl") != null && (gn.Contains("great husk sentry") || gn.Contains("great shield zombie"))) return true;   // Great Husk Sentry
            PlayMakerFSM km = FsmByName(g, "Guard");                   // Kingsmould
            if (km != null && HasState(km, "Throw Catch")) return true;
            PlayMakerFSM h = FsmByName(g, "Hopper");                   // Great Hopper
            return h != null && HasState(h, "Jump Anticipate Alert");
        }
        private bool IsBoss(HealthManager hm)
        {
            if (hm == null) return false;
            if (_bigNormals.Contains(hm)) return false;
            if (hm.hp > MaxInterruptHp && IsBigNormalEnemy(hm.gameObject)) { _bigNormals.Add(hm); return false; }
            if (hm.hp > MaxInterruptHp) { _knownBosses.Add(hm); return true; }
            return _knownBosses.Contains(hm);
        }

        private void RefreshBossControls()
        {
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (!IsBoss(hm)) continue;
                if (_sheoStunSound == null && hm.gameObject.name.ToLowerInvariant().Contains("sheo"))
                    CacheSheoStunSound(hm.gameObject);
                PlayMakerFSM m = MainFsm(hm.gameObject);
                if (m != null && !_bossControls.Contains(m)) _bossControls.Add(m);
            }
            _bossControls.RemoveAll(x => x == null);
        }

        // Kısa donma sersemlemesi: durum makinesi kısa süre durur (baştan başlamaz), boss kıpırdamaz,
        // çarpma hasarı kapalı, beyaz yanıp söner. Sonra açıkça Idle'a geçirilir (takılma olmasın diye).
        private readonly System.Collections.Generic.HashSet<GameObject> _frozen = new System.Collections.Generic.HashSet<GameObject>();

        private IEnumerator FreezeStun(GameObject boss, GameObject hitbox)
        {
            if (boss == null || !_frozen.Add(boss)) yield break;
            PlayMakerFSM m = MainFsm(boss);
            if (m == null) { _frozen.Remove(boss); yield break; }

            string from = m.ActiveStateName;
            Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
            tk2dSpriteAnimator anim = boss.GetComponent<tk2dSpriteAnimator>();
            tk2dSprite sprite = boss.GetComponent<tk2dSprite>();
            Color origColor = sprite != null ? sprite.color : Color.white;

            // Çarpma/kılıç hasarını kapat.
            // Not: oyun DamageHero'nun "enabled" ayarına bakmıyor, hasar miktarını 0 yapmak gerekiyor.
            var damagers = new System.Collections.Generic.List<DamageHero>();
            var damageVals = new System.Collections.Generic.List<int>();
            foreach (DamageHero dh in boss.GetComponentsInChildren<DamageHero>())
            {
                if (dh.damageDealt > 0) { damagers.Add(dh); damageVals.Add(dh.damageDealt); dh.damageDealt = 0; }
            }

            // Sersemleme animasyonu ve sesi (durum makinesi durmadan önce okunur).
            string clip = FindStateValue(m, boss.name, StunAnimCandidates, true);
            string launchClip = FindStateValue(m, boss.name, StunLaunchAnimCandidates, true);
            AudioClip sound = null;
            string soundState = FindStateValue(m, boss.name, StunSoundCandidates, false);
            if (soundState != null) sound = GetStateAudio(m, soundState);
            AudioClip stunSound = FindStunSound();
            if (stunSound != null) { sound = stunSound; soundState = "oyunun sersemleme sesi"; }
            else if (_sheoStunSound != null) { sound = _sheoStunSound; soundState = "Sheo'nun sersemleme sesi"; }

            m.Fsm.RestartOnEnable = false;
            m.enabled = false;

            if (sound != null) PlayFullSound(sound);
            // Bossun üstünde büyük beyaz patlama (diğer bossların sersemleme efektine benzer).
            if (StunBurstEnabled) GameManager.instance.StartCoroutine(StunBurst(BossCenter(boss)));
            // Sersemleme sarsıntısı.
            try { GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake"); } catch (System.Exception) { }

            // 1) Diğer bosslardaki gibi önce kısa bir savrulma: havaya hafifçe sıçrayıp geri düşer ("Stun Air"),
            // 2) sonra yere çöküp sersemler ("Stun").
            if (anim != null && launchClip != null)
            {
                bool launched = true;
                try { anim.Play(launchClip); } catch (System.Exception) { launched = false; }
                if (launched)
                {
                    Vector3 start = boss.transform.position;
                    float dir = (HeroController.instance != null
                                 && HeroController.instance.transform.position.x > start.x) ? -1f : 1f;
                    // İç içe kaldıysanız boss, gövdesi senden ayrılacak kadar geri savrulsun.
                    float back = Mathf.Max(StunLaunchBack, OverlapPushNeeded(boss) + 0.3f);
                    float t0 = Time.time;
                    while (boss != null && Time.time - t0 < StunLaunchTime)
                    {
                        float k = (Time.time - t0) / StunLaunchTime;
                        boss.transform.position = new Vector3(start.x + dir * back * k,
                            start.y + 4f * StunLaunchHeight * k * (1f - k), start.z);
                        if (rb != null) rb.velocity = Vector2.zero;
                        yield return null;
                    }
                    if (boss != null) boss.transform.position = new Vector3(start.x + dir * back, start.y, start.z);
                }
            }
            if (boss == null) { _frozen.Remove(boss); yield break; }

            if (anim != null)
            {
                if (clip != null) { try { anim.Play(clip); } catch (System.Exception) { anim.Pause(); clip = null; } }
                else anim.Pause();
            }

            float end = Time.time + FreezeStunTime;
            while (boss != null && Time.time < end)
            {
                if (rb != null) rb.velocity = new Vector2(0f, Mathf.Min(rb.velocity.y, 0f));
                if (sprite != null)
                {
                    float k = Mathf.PingPong(Time.time * 8f, 1f);
                    sprite.color = Color.Lerp(origColor, Color.white, 0.6f * k);
                }
                yield return null;
            }

            if (boss != null)
            {
                if (sprite != null) sprite.color = origColor;
                if (anim != null && clip == null) anim.Resume();
                KillHitbox(boss, hitbox);
                if (m != null)
                {
                    m.enabled = true;
                    string idle = HasState(m, "Idle") ? "Idle" : null;
                    if (idle != null) m.SetState(idle);
                }
                // Knight hâlâ bossun içindeyse çarpma hasarını açma: ayrılana kadar (en fazla 1.5 sn) bekle.
                float waitUntil = Time.time + 1.5f;
                while (boss != null && Time.time < waitUntil && OverlapPushNeeded(boss) > 0f)
                    yield return null;
                for (int i = 0; i < damagers.Count; i++) if (damagers[i] != null) damagers[i].damageDealt = damageVals[i];
            }
            _frozen.Remove(boss);
        }

        private IEnumerator SlyNativeStun(GameObject boss, GameObject hitbox)
        {
            if (boss == null || !_frozen.Add(boss)) yield break;
            PlayMakerFSM m = MainFsm(boss);
            if (m == null) { _frozen.Remove(boss); yield break; }

            float start = Time.time;
            string from = m.ActiveStateName;
            m.SendEvent("STUN");
            // Saldırı yarıda kesildiği için oyun dash kılıç vuruş alanını kapatamıyor: biz kapatıyoruz.
            KillHitbox(boss, hitbox);
            yield return null;
            KillHitbox(boss, hitbox);

            // Yerdeki sersemleme durumuna ("Stun") gelince kısa bekleyip kalkışı başlat.
            float timeout = Time.time + 4f;
            while (boss != null && m != null && m.ActiveStateName != "Stun" && Time.time < timeout)
                yield return null;
            if (boss != null && m != null && m.ActiveStateName == "Stun")
            {
                yield return new WaitForSeconds(SlyStunGroundTime);
                if (m != null && m.ActiveStateName == "Stun")
                {
                    m.SendEvent("END");
                    StopLoopingSounds(boss);
                }
            }

            // Tamamen kalkana kadar bekle (teşhis için süreyi yaz).
            timeout = Time.time + 6f;
            while (boss != null && m != null && m.ActiveStateName != "Idle" && Time.time < timeout)
                yield return null;
            if (boss != null) StopLoopingSounds(boss);
            if (m != null)
                { }
            _frozen.Remove(boss);
        }

        // Aday listesinden bu bossa uyan ilk durumu bulur. wantClip=true ise o durumun animasyon adını,
        // false ise durum adını döndürür (sesi GetStateAudio okur).
        private static string FindStateValue(PlayMakerFSM m, string bossName, string[] candidates, bool wantClip)
        {
            string lower = bossName.ToLowerInvariant();
            foreach (string r in candidates)
            {
                int bar = r.IndexOf('|');
                if (bar < 0 || !lower.Contains(r.Substring(0, bar))) continue;
                string state = r.Substring(bar + 1);
                if (!HasState(m, state)) continue;
                if (!wantClip) { if (GetStateAudio(m, state) != null) return state; continue; }
                string clip = GetStateClip(m, state);
                if (!string.IsNullOrEmpty(clip)) return clip;
            }
            return null;
        }

        // Bir durumun oynattığı tk2d animasyon adı (Tk2dPlayAnimation / ...WithEvents eylemindeki clipName).
        private static string GetStateClip(PlayMakerFSM m, string stateName)
        {
            foreach (var st in m.FsmStates)
            {
                if (st.Name != stateName || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || !a.GetType().Name.StartsWith("Tk2dPlayAnimation")) continue;
                    var f = a.GetType().GetField("clipName");
                    var v = f != null ? f.GetValue(a) as HutongGames.PlayMaker.FsmString : null;
                    if (v != null && !string.IsNullOrEmpty(v.Value)) return v.Value;
                }
            }
            return null;
        }

        // Bir durumun çaldığı ses (AudioPlayerOneShotSingle eylemindeki audioClip).
        // Parry'lenen saldırının vuruş alanını kapatır (bossun kendisi değilse). Oyun bir sonraki saldırıda
        // kendisi yeniden açar. Saldırı yarıda kesilince açık kalıp seni vurmaya devam etmesin diye.
        private void KillHitbox(GameObject boss, GameObject hitbox)
        {
            try
            {
                if (hitbox == null || boss == null || hitbox == boss || !hitbox.activeSelf) return;
                if (!hitbox.transform.IsChildOf(boss.transform)) return;
                hitbox.SetActive(false);
            }
            catch (System.Exception) {  }
        }

        // Sesi baştan sona çalar. (Eski yöntem hitstop yavaşlatmasında sesi erken kesiyordu.)
        private void PlayFullSound(AudioClip clip) { PlayFullSound(clip, StunSoundVolume); }


        // Oyunun ana ses ve efekt sesi ayarı (0-10) -> 0-1 çarpan.
        private static float GameSfxScale()
        {
            try
            {
                var gs = GameManager.instance.gameSettings;
                return Mathf.Clamp01(gs.masterVolume / 10f) * Mathf.Clamp01(gs.soundVolume / 10f);
            }
            catch (System.Exception) { return 1f; }
        }

        private void PlayFullSound(AudioClip clip, float volume)
        {
            try
            {
                GameObject g = new GameObject("ParryStunSound");
                if (GameCameras.instance != null) g.transform.position = GameCameras.instance.transform.position;
                AudioSource src = g.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 0f; // uzaklıktan bağımsız
                // Oyunun efekt sesi kanalına bağla: oyundaki ses ayarlarına uyar, diğer seslerle dengeli çalar.
                var grp = GameSfxGroup();
                if (grp != null) { src.outputAudioMixerGroup = grp; src.volume = volume; }
                else src.volume = volume * GameSfxScale();
                src.Play();
                GameManager.instance.StartCoroutine(DestroyAfterRealtime(g, clip.length + 0.3f));
            }
            catch (System.Exception) {  }
        }

        private static UnityEngine.Audio.AudioMixerGroup _sfxGroup;
        private UnityEngine.Audio.AudioMixerGroup GameSfxGroup()
        {
            if (_sfxGroup != null) return _sfxGroup;
            try
            {
                HeroController h = HeroController.instance;
                if (h != null)
                    foreach (AudioSource a in h.GetComponentsInChildren<AudioSource>(true))
                        if (a != null && a.outputAudioMixerGroup != null) { _sfxGroup = a.outputAudioMixerGroup; break; }
            }
            catch (System.Exception) {  }
            return _sfxGroup;
        }

        private IEnumerator DestroyAfterRealtime(GameObject g, float t)
        {
            yield return new WaitForSecondsRealtime(t);
            if (g != null) UnityEngine.Object.Destroy(g);
        }

        // Sheo dövüşündeyken onun sersemleme sesini saklar; böylece sonra Oro/Mato'da da kullanılabilir.
        private const float StunSoundVolume = 0.35f; // sersemleme sesinin yüksekliği (0-1)
        private static AudioClip _sheoStunSound;
        private void CacheSheoStunSound(GameObject sheo)
        {
            foreach (PlayMakerFSM fsm in sheo.GetComponents<PlayMakerFSM>())
            {
                if (fsm.FsmName != "Stun Control" && !fsm.FsmName.ToLowerInvariant().Contains("stun")) continue;
                foreach (var st in fsm.FsmStates)
                {
                    AudioClip c = GetStateAudio(fsm, st.Name);
                    if (c == null) continue;
                    _sheoStunSound = c;
                    return;
                }
            }
        }

        // Sly'ın sersemleme sonrası tekrar eden "başı ağrıyor" sesini durdurur.
        private void StopLoopingSounds(GameObject boss)
        {
            // Ses Sly'ın kendi ses kaynağından ya da yakındaki bir "Audio Player" nesnesinden çalıyor olabilir.
            // Tek seferlik çalınan seslerde clip alanı boş kalır, o yüzden adına bakmadan hepsini susturuyoruz.
            Vector3 bp = boss.transform.position;
            int n = 0;
            var names = new System.Collections.Generic.List<string>();
            foreach (AudioSource a in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (a == null || a.gameObject.name == "ParryStunSound") continue;
                bool own = a.transform.IsChildOf(boss.transform);
                bool player = a.gameObject.name.ToLowerInvariant().Contains("audio player")
                              && Vector2.Distance(a.transform.position, bp) < 15f;
                if (!own && !player) continue;
                if (a.isPlaying) names.Add((a.clip != null ? a.clip.name : "tek seferlik") + " <- " + a.gameObject.name);
                a.Stop();
                n++;
            }
        }

        // Bellekte yüklü seslerden bir "sersemleme" sesi arar. Bulduklarını bir kez log'a yazar.
        private static AudioClip _stunSoundCache;
        private bool _stunSoundsLogged;
        private AudioClip FindStunSound()
        {
            if (_stunSoundCache != null) return _stunSoundCache;
            AudioClip best = null;
            int bestScore = -1;
            var names = new System.Collections.Generic.List<string>();
            foreach (AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (c == null) continue;
                string n = c.name.ToLowerInvariant();
                if (!n.Contains("stun") && !n.Contains("stagger")) continue;
                names.Add(c.name);
                // Konuşma sesleri olmasın (Sly, Hornet vb.); genel boss sersemleme sesi tercih edilir.
                if (n.Contains("sly") || n.Contains("hornet") || n.Contains("voice") || n.Contains("zote")) continue;
                int score = n.Contains("boss") ? 3 : n.Contains("enemy") ? 2 : 1;
                if (score > bestScore) { best = c; bestScore = score; }
            }
            if (!_stunSoundsLogged)
            {
                _stunSoundsLogged = true;
            }
            _stunSoundCache = best;
            return best;
        }

        private static AudioClip GetStateAudio(PlayMakerFSM m, string stateName)
        {
            foreach (var st in m.FsmStates)
            {
                if (st.Name != stateName || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || a.GetType().Name != "AudioPlayerOneShotSingle") continue;
                    var f = a.GetType().GetField("audioClip");
                    var v = f != null ? f.GetValue(a) as HutongGames.PlayMaker.FsmObject : null;
                    AudioClip c = v != null ? v.Value as AudioClip : null;
                    if (c != null) return c;
                }
            }
            return null;
        }


        // Sheo pembe saldırı parry'si: oyunun kendi sersemlemesi, ama en geç ShortStunTime'da kalkar.
        private void ShortStun(GameObject boss)
        {
            try
            {
                PlayMakerFSM m = MainFsm(boss);
                if (m == null) return;
                m.SendEvent("STUN");
                GameManager.instance.StartCoroutine(ShortStunRoutine(boss, m));
            }
            catch (System.Exception e) { Log("HATA (kısa sersemleme): " + e); }
        }

        private IEnumerator ShortStunRoutine(GameObject boss, PlayMakerFSM m)
        {
            yield return new WaitForSeconds(ShortStunTime);
            if (boss == null || m == null) yield break;
            string s = m.ActiveStateName;
            if ((s == "Stun Reset" || s == "Stun Launch" || s == "Stun Air" || s == "Stun Land")
                && HasState(m, "Stun Recover"))
            {
                m.SetState("Stun Recover");
            }
            else
            {
            }
        }

        private string UnparryableBossState(GameObject go)
        {
            string s = GetControlState(go);
            if (s != null)
            {
                GameObject body = BodyKey(go);
                string name = body != null ? body.name : null;
                bool locked = System.Array.IndexOf(UnparryableBossStates, s) >= 0
                              || MatchRule(UnparryableBossRules, name, s);
                return locked ? s : null;
            }

            foreach (PlayMakerFSM c in _bossControls)
            {
                if (c == null) continue;
                string cs = c.ActiveStateName;
                if (System.Array.IndexOf(UnparryableBossStates, cs) >= 0
                    || MatchRule(UnparryableBossRules, c.gameObject.name, cs)) return cs;
            }
            return null;
        }

        // ================= SLY, SHEO, ORO & MATO =================

        // Bu bossların GÖVDESİ (bossun kendisi) parry'lenemez, kılıçları parry'lenebilir.
        private static readonly string[] BodyUnparryableBosses = { "oro", "mato" };

        // Bu durumlarda sadece bossa BAĞLI vuruşlar (gövde, yere çarpma) parry'lenemez.
        // Bossa bağlı olmayanlar (ör. Sheo'nun kırmızı boyaları) parry'lenebilir.
        private static readonly string[] UnparryableAttachedRules =
        {
            // Sheo kırmızı saldırı: zıplayıp boya atma ve aşağı dalış
            "sheo|JumpSlash1", "sheo|JumpSlash2", "sheo|JumpSlash3", "sheo|Fire Dir",
            "sheo|RedShot L", "sheo|RedShot R", "sheo|Dstab Antic", "sheo|Dstab",
            "sheo|Dstab Land", "sheo|Dstab Recover",
            // Soul Master / Soul Tyrant: gövdesi sadece sana doğru uçarken parry'lenebilir (fırlattığı ruh topları her zaman)
            "mage lord|Init", "mage lord|Idle", "mage lord|Teleport", "mage lord|Tele Line", "mage lord|Tele Away", "mage lord|Pause", "mage lord|Away Valid?", "mage lord|Tele Top Y", "mage lord|Tele Spinner X", "mage lord|After Tele", "mage lord|HS Summon", "mage lord|HS Orb", "mage lord|HS Dir", "mage lord|HS Right", "mage lord|HS Left", "mage lord|HS Stop", "mage lord|HS Tele Out", "mage lord|HS Ret Left", "mage lord|Hs Ret Right", "mage lord|Reactivate", "mage lord|HS Dissipate", "mage lord|Tele Charge", "mage lord|Charge Stop", "mage lord|Tele Bot Y", "mage lord|Set Shots", "mage lord|Shot Antic", "mage lord|Shot", "mage lord|Tele Shoot", "mage lord|Attack Choice", "mage lord|Next?", "mage lord|Damaged Idle", "mage lord|Set Idle Timer", "mage lord|Shoot Valid?", "mage lord|Tele Quake", "mage lord|TeleportQ", "mage lord|Tele Line Q", "mage lord|Quake Antic", "mage lord|Quake Down", "mage lord|Quake Land", "mage lord|Re Pos", "mage lord|Quake Waves", "mage lord|Damage Tele Away", "mage lord|Attack Instead", "mage lord|Charge Antic", "mage lord|Fakeout?", "mage lord|Fake Down", "mage lord|Reset", "mage lord|Stun Init", "mage lord|Stun L", "mage lord|Stun R", "mage lord|Stun Start 1", "mage lord|Stun Start 2", "mage lord|Stunned", "mage lord|Stun Reset", "mage lord|Stun Hit", "mage lord|Stun Hit L", "mage lord|Stun Hit R", "mage lord|Sleep", "mage lord|Set Entry Point", "mage lord|Teleport In", "mage lord|Roar", "mage lord|Roar End", "mage lord|Encountered?", "mage lord|Quick Start", "mage lord|Music", "mage lord|GG Enter"
        };

        // Odaklanıp attıkları dash senin hizana gelince biter (oyunun kendi "END" sinyaliyle,
        // ardından dash sonu kılıç vuruşu gelir).
        private static readonly string[] DashStopRules =
        {
            "sly|Dashing L", "sly|Dashing R", "oro|Dashing L", "oro|Dashing R", "mato|Dashing L", "mato|Dashing R",
            // Dash sonu kılıç vuruşu da ileri kayıyor (Sly'da "D Slash Speed"): onu da senin hizanda durdur.
            "sly|D Slash S1", "sly|DSlash S2", "oro|D Slash", "mato|D Slash"
        };
        // Dash, senin bu kadar önünde biter (dash sonu kılıcı sana yetişsin diye).
        private const float DashStopOffset = 3f;

        // Sheo mavi saldırı: başarılı parry'den sonra ek dokunulmazlık (boya değmesin diye).
        private static readonly string[] ExtraInvulnRules =
        {
            "sheo|Slash Antic", "sheo|S 1", "sheo|Slash Dir", "sheo|Slash L", "sheo|Slash R",
            "sheo|S 2", "sheo|S 3", "sheo|Slash Recover", "sheo|Slash Recover 2"
        };
        private static readonly string[] ExtraInvulnNames = { "paint shot b" }; // mavi boyalar
        private const float ExtraInvulnTime = 0.35f;
        private float _extraInvulnUntil;

        // Sheo pembe saldırı (yakına yaptığı ağır, geniş vuruş): parry'lenince anında kısa sersemleme.
        private static readonly string[] ShortStunRules =
        {
            "sheo|GSlash Charge", "sheo|GSlash Charged", "sheo|Gslash1", "sheo|GSlash 2",
            "sheo|GSlash 3", "sheo|Drops", "sheo|GSlash 4"
        };
        private const float ShortStunTime = 0.7f;

        // Sly, Oro ve Mato'nun odaklı dash'i parry'lenince kısa sersemleme (kendi yazdığımız: bu bosslarda
        // oyunun sersemlemesi yok ya da kılıç fırlatma içeriyor). Boss donar, yanıp söner, çarpmaz, sonra Idle.
        private static readonly string[] FreezeStunRules =
        {
            "sly|Dashing L", "sly|Dashing R", "sly|D Slash S1", "sly|DSlash S2",
            "oro|Dashing L", "oro|Dashing R", "oro|D Slash", "oro|D Slash Recover",
            "mato|Dashing L", "mato|Dashing R", "mato|D Slash", "mato|D Slash Recover"
        };
        private const float FreezeStunTime = 1.0f;

        // Sly: oyunun kendi sersemlemesi (kılıç fırlar, diz çöker, kılıcını alıp kalkar).
        // Yerde bekleme kısmı bu kadar sürer, sonra kalkmaya başlar. Toplam yaklaşık 2 saniye.
        private const float SlyStunGroundTime = 0.5f;

        // Sersemleme görüntüsü ve sesi bossun KENDİ durumlarından okunur (ilk bulunan kullanılır).
        private static readonly string[] StunAnimCandidates =
        {
            "sly|Stun", "sly|Stun Land", "sly|Stun Air",
            "oro|Death Land", "oro|Death Air", "mato|Death Land", "mato|Death Air"
        };
        // Sersemlemeden önceki kısa savrulma animasyonu (havada) ve ayarları.
        private static readonly string[] StunLaunchAnimCandidates =
        {
            "oro|Death Launch", "oro|Explode", "mato|Death Launch", "mato|Explode"
        };
        private const float StunLaunchTime = 0.35f;   // savrulma süresi
        private const float StunLaunchHeight = 1.2f;  // ne kadar yükseğe sıçrar
        private const float StunLaunchBack = 1.5f;    // senden ne kadar geriye gider

        private static readonly string[] StunSoundCandidates =
        {
            "sly|Stun Launch", "sly|Stun Reset",
            "oro|Death Launch", "oro|Explode", "mato|Death Launch", "mato|Explode"
        };

        // Bossa özel parry'lenemez durumlar ("boss adının parçası|durum").
        private static readonly string[] UnparryableBossRules =
        {
            // Hollow Knight 3. faz: iplerle çekilip yere çarpa çarpa takip ettiği saldırı
            "hollow knight boss|Puppet Antic", "hollow knight boss|Puppet Up", "hollow knight boss|Puppet Down",
            "hollow knight boss|PuppetSlam", "hollow knight boss|Puppet Loop", "hollow knight boss|Puppet ReUp",
            "hollow knight boss|Puppet SlAntic", "hollow knight boss|Puppet End",
            // Hornet: olduğu yerde etrafında iplik topu döndürdüğü saldırı (yerde ve havada)
            "hornet boss|Sphere Antic G", "hornet boss|Sphere", "hornet boss|Sphere Recover",
            "hornet boss|Sphere Antic A", "hornet boss|Sphere A", "hornet boss|Sphere Recover A", "hornet boss|Sphere A End",
            // Watcher Knight: zıplayarak dönen saldırı
            "black knight|Jump Start", "black knight|Aim", "black knight|Jump Antic", "black knight|Antic Air",
            "black knight|Jump Launch", "black knight|Jump Air", "black knight|Bounce", "black knight|Bounce Air",
            "black knight|Land", "black knight|Flip Jump Dir",
            // Sly: odaklanıp dönerek saldırı
            "sly|Cyc Jump Antic", "sly|Cyc Jump Launch", "sly|Cyc Up", "sly|Cyc Down", "sly|Cyc Charge",
            "sly|Cyc Charged", "sly|Cyclone Start", "sly|Cycloning", "sly|Cyc Decel", "sly|Cyclone End",
            // Sly: kombonun 3. parçası, yukarı dönerek çıkış
            "sly|Spin Slash Launch", "sly|Spin Slash Air", "sly|After SpinSlash", "sly|Spin Slash End",
            "sly|SpinStomp Antic",
            // Sly: son faz (arenada sekip dönerek saldırma)
            "sly|Begin Rage", "sly|Acended HP", "sly|Flipping", "sly|Rage Dash", "sly|At Hero?",
            "sly|Bounce L", "sly|Bounce R", "sly|Bounce D", "sly|Bounce U",
            "sly|Fire L", "sly|Fire R", "sly|Fire D", "sly|Fire U",
            "sly|Rage Slash Antic", "sly|Rage Slash", "sly|Rage Slash CD", "sly|Launch Away",
            // Sheo sarı saldırı: odaklanıp uzaktan atıldığı saplama
            "sheo|Stab Antic", "sheo|Stab1", "sheo|Stab2", "sheo|Stab Recover",
            // Mato (ve Oro) dönme saldırısı
            "mato|Cyclone Start", "mato|Cyclone", "mato|Cyc Dir", "mato|Cyc L", "mato|Cyc R", "mato|Cyclone Up",
            "oro|Cyclone Start", "oro|Cyclone", "oro|Cyc Dir", "oro|Cyc L", "oro|Cyc R", "oro|Cyclone Up",

            // Gruz Mother: aşağı yukarı sürekli çarptığı saldırı
            "giant fly|Slam Antic", "giant fly|Check Direction", "giant fly|Go Left", "giant fly|Go Right",
            "giant fly|Launch Up", "giant fly|Launch Down", "giant fly|Flying", "giant fly|Turn Left", "giant fly|Turn Right",
            "giant fly|Slam Down", "giant fly|Slam Up",

            // Grimm balon saldırısı (iki versiyon): boss bu adımlardayken hiçbir şey parry'lenemez.
            "grimm|Balloon Tele In", "grimm|Balloon Antic", "grimm|Music", "grimm|Halfway?",
            "grimm|Inflate", "grimm|Fire Pause", "grimm|Fire Repeat",
            "grimm|Fire Low R", "grimm|Fire Low L", "grimm|Fire Mid R", "grimm|Fire Mid L",
            "grimm|Fire High R", "grimm|Fire High L", "grimm|Alt", "grimm|Down",
            "grimm|End Pause", "grimm|Deflate"
        };


        // Grimm saldırı iptali: dash/pençe hızı kesilir, vuruş kutuları kapanır, Grimm sana dönüp
        // kendi örümcek gibi geri kayma hareketini (Evade) yapar. Sonra:
        //   firebatsAfter = false -> ışınlanarak kaybolur (TELE OUT)
        //   firebatsAfter = true  -> ışınlanmadan hemen alev yarasası saldırısına geçer
        private void EvadeCancel(GameObject boss, string from, bool firebatsAfter)
        {
            try
            {
                PlayMakerFSM control = null;
                foreach (PlayMakerFSM fsm in boss.GetComponents<PlayMakerFSM>())
                {
                    if (fsm.FsmName == "Control") { control = fsm; break; }
                }
                if (control == null) {  return; }

                // Hızı kes, yoksa geri kayma hazırlığında ileri kaymaya devam eder.
                Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);

                // Matkap dalışı Grimm'i çapraz döndürüyor; dik dursun.
                boss.transform.rotation = Quaternion.identity;

                // Dash'in vuruş kutusunu kapat (sonraki dash'te oyun kendisi tekrar açar).
                Transform dashSpike = boss.transform.Find("Dash Spike");
                if (dashSpike != null) dashSpike.gameObject.SetActive(false);

                // Pençe vuruş kutularının çarpışmasını kapat (oyun her pençede kendisi tekrar açar).
                foreach (string n in new[] { "Slash1", "Slash2", "Slash3" })
                {
                    Transform t = boss.transform.Find(n);
                    if (t == null) continue;
                    foreach (Collider2D c in t.GetComponents<Collider2D>()) c.enabled = false;
                }

                if (HasState(control, "Evade Antic"))
                {
                    // Daha hızlı örümcek kayması (kayma bitince eski haline döner).
                    var evadeSpeed = control.FsmVariables.FindFsmFloat("Evade Speed");
                    float origEvade = evadeSpeed != null ? evadeSpeed.Value : 0f;
                    if (evadeSpeed != null) evadeSpeed.Value = origEvade * GrimmEvadeSpeedMul;
                    _grimmEvadeRestore = evadeSpeed;
                    _grimmEvadeOrig = origEvade;

                    control.SetState("Evade Antic");
                    GameManager.instance.StartCoroutine(AfterEvade(boss, control, firebatsAfter));
                }
                else
                {
                    control.SendEvent("TELE OUT");
                    GameManager.instance.StartCoroutine(AfterCancelCheck(boss, control));
                }
            }
            catch (System.Exception e) { Log("HATA (Grimm iptali): " + e); }
        }

        // Geri kayma bitene kadar bekler, sonra sıradaki hareketi başlatır. Haritaya göre kaymadan
        // sonra Grimm normalde pençe ya da yarasa saldırısını kendisi seçiyor; biz araya girip seçiyoruz.
        private IEnumerator AfterEvade(GameObject boss, PlayMakerFSM control, bool firebatsAfter)
        {
            bool sawEvadeEnd = false;
            float timeout = Time.time + 1.5f; // bir şey ters giderse yine de devam etsin
            while (boss != null && control != null && Time.time < timeout)
            {
                string s = control.ActiveStateName;
                if (s == "Evade End") sawEvadeEnd = true;
                else if (sawEvadeEnd) break;                       // kayma bitti
                else if (s != "Evade Antic" && s != "Evade") break; // beklenmedik bir duruma geçti
                yield return null;
            }

            // Kayma hızını eski haline getir.
            if (_grimmEvadeRestore != null) { _grimmEvadeRestore.Value = _grimmEvadeOrig; _grimmEvadeRestore = null; }

            if (boss == null || control == null) yield break;
            // Parry stun yedirdiyse: ceza (hızlı yarasa / ışınlanma) yok, stun devam etsin.
            if (control.ActiveStateName.Contains("Stun") || GrimmStunned(boss))
            {
                yield break;
            }

            string next = firebatsAfter
                ? (HasState(control, "To Firebats") ? "To Firebats" : "FB Antic")
                : null;

            if (next != null && HasState(control, next))
            {
                // Parry'nin bedeli: bu yarasa saldırısı daha hızlı (bitince normale döner).
                // Normal Grimm'de ceza saldırısı kendi normal hızında olur (sadece Nightmare King Grimm'de hızlı).
                bool fastPunish = boss.name.ToLowerInvariant().Contains("nightmare");
                var waits = ScaleWaits(control, PunishFirebatStates, fastPunish ? PunishFirebatWaitMul : 1f);
                var batSpeed = control.FsmVariables.FindFsmFloat("Firebat Speed");
                float origBat = batSpeed != null ? batSpeed.Value : 0f;
                if (batSpeed != null) batSpeed.Value = origBat * (fastPunish ? PunishFirebatSpeedMul : 1f);
                _grimmTempMods++;

                control.SetState(next);
                GameManager.instance.StartCoroutine(RestoreFirebats(control, waits, batSpeed, origBat));
            }
            else if (GrimmDashParryAmbush && HasState(control, "Slash Tele In") && HasState(control, "Out Pause"))
            {
                // Zorlaştırma: ışınlanır ışınlanmaz arkanda belirip pençe saldırısı yapar.
                GameManager.instance.StartCoroutine(TeleBehindAndClaw(boss, control));
                yield break;
            }
            else
            {
                control.SendEvent("TELE OUT");
            }
            GameManager.instance.StartCoroutine(AfterCancelCheck(boss, control));
        }

        // ---------- Rüya iğnesi güçlendirmesi ----------
        // Uyanmış (en üst seviye) rüya iğnesiyle bir düşmana vurunca sonraki 5 sn iğne hasarı %15 artar.
        // Rüya iğnesinin işe yaramadığı bosslarda olmaz (ruh vermeyenler ve aşağıdaki liste).
        private static readonly string[] DreamBuffExcluded =
        {
            "radiance",          // Radiance ve Absolute Radiance
            "hk prime",          // Pure Vessel
            "hollow knight",     // Hollow Knight
            "infected knight",   // Broken Vessel
            "lost kin"           // Lost Kin
        };
        private const float DreamNailBuffTime = 2f;          // normal süre
        private const float DreamNailBuffTimeWielder = 3f;   // Dream Wielder takılıyken
        private const float DreamNailBuffMul = 1.10f;
        private float _dreamBuffUntil = -10f;
        private float _dreamBuffStart = -10f;

        // Güçlendirme süresince iğne savuruşları rüya rengine (açık mor) yumuşakça geçer, sonunda yavaşça söner.
        private static readonly Color DreamNailGlow = new Color(0.82f, 0.62f, 1f, 1f);
        private const float DreamGlowFadeIn = 0.35f;
        private const float DreamGlowFadeOut = 0.8f;
        private System.Collections.Generic.List<tk2dSprite> _slashSprites;
        private System.Collections.Generic.List<Color> _slashOrig;
        private bool _slashTinted;

        // Güçlendirme süresince her iğne savuruşunda vuruş yönüne 2-3 küçük rüya özü baloncuğu çıkar.
        private static Texture2D _essenceTex;
        private static Sprite _essenceSprite;
        private static readonly Color EssenceColor = new Color(1f, 0.84f, 0.88f, 1f);

        // Oyundaki rüya özü gibi: ince kenarlı daire, içinde iç içe geçmiş çiçek desenli halkalar, yumuşak parıltı.
        private static Sprite EssenceSprite()
        {
            if (_essenceSprite != null) return _essenceSprite;
            int n = 128; _essenceTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            const float R = 0.82f;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 0f;
                if (r < R)
                {
                    for (int k = 0; k < 12; k++)   // 12 halkalı çiçek deseni
                    {
                        float ang = k * Mathf.PI / 6f;
                        float cx = Mathf.Cos(ang) * R * 0.5f, cy = Mathf.Sin(ang) * R * 0.5f;
                        float d = Mathf.Sqrt((dx - cx) * (dx - cx) + (dy - cy) * (dy - cy)) - R * 0.5f;
                        a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(d) / 0.022f) * 0.75f);
                    }
                    a = Mathf.Max(a, Mathf.Pow(Mathf.Clamp01(1f - r / R), 1.5f) * 0.4f);   // içi hafif dolu
                }
                a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(r - R) / 0.035f));                 // dış kenar
                a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(r - R * 0.93f) / 0.015f) * 0.5f);  // iç kenar
                a = Mathf.Max(a, Mathf.Pow(Mathf.Clamp01(1f - r), 2f) * 0.5f);                   // parıltı
                _essenceTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
            _essenceTex.Apply();
            _essenceSprite = Sprite.Create(_essenceTex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 128f);   // 1 birim
            return _essenceSprite;
        }

        // Uyanmış rüya iğnesinin envanter açıklamasına güçlendirmeyi anlatan bir cümle eklenir.
        private const string DreamNailDescExtra = "Striking a foe with it briefly empowers the wielder's nail.";
        // =====================================================================
        //  Shinobi's Charm (SFCore ile eklenen yeni tılsım, 3 çentik)
        //  - Parry penceresi 0,38 sn, max saldırıların dar penceresi 0,3 sn
        //  - Parry'nin sersemleme katkısı 1,25
        //  - Bossun parry'lenemeyen saldırısı başlarken başının üstünde tehlike dalgası (Fury'nin "Rage Burst Effect"i)
        //  Howling Cliffs girişinde (Cliffs_01) yerde durur. Simge: Mods\ParryMod\Shinobi_Charm.png
        // =====================================================================
        private const string ShinobiName = "Shinobi's Charm";
        private const string ShinobiDesc = "A charm carried into Hallownest from a distant land beyond the edge of the kingdom, by a warrior who lived by the blade and the shadow.<br><br>Its bearer can see danger coming, and their deflections come easier. Stunnable foes are easier to stun.";
        private const int ShinobiCost = 3;
        private const string ShinobiScene = "Cliffs_01";
        private static readonly Vector2 ShinobiPos = new Vector2(75.24f, 3.41f);   // PlaceScout ile ölçüldü (Knight'ın durduğu yer)
        private const float ShinobiIconPPU = 80f;       // envanterdeki boyut (küçük sayı = büyük)
        private int _shinobiId = -1;
        private ParryLocalSettings _local = new ParryLocalSettings();

        public void OnLoadLocal(ParryLocalSettings s) { _local = s ?? new ParryLocalSettings(); }
        public ParryLocalSettings OnSaveLocal() { return _local; }

        private bool ShinobiOn => _shinobiId > 0 && _local != null && _local.ShinobiEquipped;

        private void ShinobiInit()
        {
            try
            {
                string dir = ParryAssets.ModDir;
                string path = ParryAssets.File("ShinobiCharm", "Shinobi_Charm.png");
                if (!System.IO.File.Exists(path)) {  return; }
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                _shinobiSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ShinobiIconPPU);
                var ids = SFCore.CharmHelper.AddSprites(_shinobiSprite);
                _shinobiId = ids[0];
                ModHooks.GetPlayerBoolHook += ShinobiGetBool;
                ModHooks.SetPlayerBoolHook += ShinobiSetBool;
                ModHooks.GetPlayerIntHook += ShinobiGetInt;
                LoadDangerFrames(dir);
            }
            catch (System.Exception e) { Log("HATA (Shinobi's Charm eklenemedi): " + e); }
        }

        // Mods\ParryMod\Uyari\kare_01.png ... kare_05.png (557x408, oyunun kareleriyle aynı çerçeve).
        private void LoadDangerFrames(string modDir)
        {
            DangerFade.Custom.Clear();
            string dir = ParryAssets.Dir("ShinobiCharm");
            for (int i = 1; i <= 5; i++)
            {
                string path = System.IO.Path.Combine(dir, "kare_" + i.ToString("00") + ".png");
                if (!System.IO.File.Exists(path)) continue;
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                Sprite sp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 64f);
                sp.name = "shinobi_danger_" + i;
                DangerFade.Custom["rage_burst_effect" + (i - 1).ToString("000")] = sp;
            }
        }

        private void ShinobiUnload()
        {
            ModHooks.GetPlayerBoolHook -= ShinobiGetBool;
            ModHooks.SetPlayerBoolHook -= ShinobiSetBool;
            ModHooks.GetPlayerIntHook -= ShinobiGetInt;
        }

        private Sprite _shinobiSprite;

        // Godseeker modunda bütün tılsımlar alınmış başlıyor: Shinobi's Charm da alınmış başlar (takılı değil; kayıt başına bir kez).
        private void GodseekerCharmCheck(HeroController hc)
        {
            if (_shinobiId <= 0 || _local == null || _local.GodseekerGiven) return;
            PlayerData pd = PlayerData.instance;
            if (pd == null || !pd.GetBool("bossRushMode")) return;
            _local.GodseekerGiven = true;
            _local.ShinobiGot = true;
            _local.ShinobiNew = false;
        }

        private bool ShinobiGetBool(string name, bool orig)
        {
            if (_shinobiId <= 0) return orig;
            if (name == "gotCharm_" + _shinobiId) return _local.ShinobiGot;
            if (name == "newCharm_" + _shinobiId) return _local.ShinobiNew;
            if (name == "equippedCharm_" + _shinobiId) return _local.ShinobiEquipped;
            return orig;
        }

        private bool ShinobiSetBool(string name, bool value)
        {
            if (_shinobiId > 0)
            {
                if (name == "gotCharm_" + _shinobiId)
                {
                    if (value && !_local.ShinobiGot) { _local.ShinobiNew = true;  }
                    _local.ShinobiGot = value;
                }
                else if (name == "newCharm_" + _shinobiId) _local.ShinobiNew = value;
                else if (name == "equippedCharm_" + _shinobiId) { _local.ShinobiEquipped = value;  }
            }
            return value;
        }

        private int ShinobiGetInt(string name, int orig)
        {
            if (_shinobiId > 0 && name == "charmCost_" + _shinobiId) return ShinobiCost;
            return orig;
        }

        // Howling Cliffs girişinde oyunun kendi parıltılı eşyası: "Inspect" yazısı, Knight eğilip alır,
        // sağ altta tılsım bildirimi çıkar. King's Pass'teki Fury of the Fallen parıltısının kopyası,
        // içindeki tılsım numarası bizimkiyle değiştiriliyor.
        private const string ShinyScene = "Tutorial_01";
        private const string ShinyPath = "_Props/Chest/Item/Shiny Item (1)";
        private static GameObject _shinyPrefab;

        private IEnumerator ShinobiPickupRoutine()
        {
            yield return null; yield return null;
            if (_shinobiId <= 0 || _local.ShinobiGot) yield break;
            if (_shinyPrefab == null) {  yield break; }

            GameObject go = UnityEngine.Object.Instantiate(_shinyPrefab);
            go.name = "Shinobi Charm Shiny (ParryMod)";
            // Oyunun "bu sahnede alındı" kaydını kullanmasın (Cliffs_01'deki başka eşyayla karışmasın); alınma bilgisini biz tutuyoruz.
            var pbi = go.GetComponent<PersistentBoolItem>();
            if (pbi != null) UnityEngine.Object.DestroyImmediate(pbi);
            go.transform.position = new Vector3(ShinobiPos.x, ShinobiPos.y, 0f);

            PlayMakerFSM sc = null;
            foreach (PlayMakerFSM f in go.GetComponents<PlayMakerFSM>()) if (f.FsmName == "Shiny Control") sc = f;
            if (sc == null) {  UnityEngine.Object.Destroy(go); yield break; }
            var v = sc.FsmVariables;
            var pd = v.FindFsmString("PD Bool Name"); if (pd != null) pd.Value = "gotCharm_" + _shinobiId;
            var cid = v.FindFsmInt("Charm ID"); if (cid != null) cid.Value = _shinobiId;
            var ch = v.FindFsmBool("Charm"); if (ch != null) ch.Value = true;
            var tute = v.FindFsmBool("Show Charm Tute"); if (tute != null) tute.Value = false;
            var fling = v.FindFsmBool("Fling On Start"); if (fling != null) fling.Value = false;
            go.SetActive(true);

            // Yere otursun: kısa süre yerçekimiyle düşer, durunca sabitlenir.
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0.85f;
                float t0 = Time.time;
                while (go != null && Time.time - t0 < 2f)
                {
                    yield return new WaitForFixedUpdate();
                    if (go == null) yield break;
                    if (Time.time - t0 > 0.2f && Mathf.Abs(rb.velocity.y) < 0.05f) break;
                    if (go.transform.position.y < ShinobiPos.y - 3f) { go.transform.position = new Vector3(ShinobiPos.x, ShinobiPos.y - 0.8f, 0f); break; }
                }
                if (go == null) yield break;
                rb.gravityScale = 0f; rb.velocity = Vector2.zero;
            }
        }

        // Tehlike uyarısı: bir boss parry'lenemeyen saldırısına başladığı an (önceki adımı parry'lenebilirken)
        // Knight'ın başının üstünde Fury of the Fallen'ın patlama dalgası bir kez oynar.
        private readonly System.Collections.Generic.Dictionary<PlayMakerFSM, bool> _dangerPrev = new System.Collections.Generic.Dictionary<PlayMakerFSM, bool>();
        private GameObject _rageBurst;





        private const float DangerSoundVolume = 0.6f;
        private const string DangerSoundName = "carefree_melody_metallic";
        private const float DangerSoundPitch = 1.15f;     // 1 = normal; büyük = daha tiz
        private const float DangerReverbDecay = 1.6f;     // yankının sönme süresi (sn); uzun = daha uzun kuyruk
        private const float DangerReverbLevel = 600f;     // yankının yüksekliği (-10000 .. 2000)

        // Uyarı sesi: biraz tiz + yankılı (oyunun efekt kanalından).
        private void PlayDangerSound(AudioClip clip)
        {
            try
            {
                GameObject g = new GameObject("ParryDangerSound");
                if (GameCameras.instance != null) g.transform.position = GameCameras.instance.transform.position;
                AudioSource src = g.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 0f;
                bool custom = clip == _dangerSound;   // kendi düzenlediğin uyari.wav: olduğu gibi çal
                src.pitch = custom ? 1f : DangerSoundPitch;
                var grp = GameSfxGroup();
                if (grp != null) { src.outputAudioMixerGroup = grp; src.volume = DangerSoundVolume; }
                else src.volume = DangerSoundVolume * GameSfxScale();
                AudioReverbFilter rv = custom ? null : g.AddComponent<AudioReverbFilter>();
                if (rv != null) {
                rv.reverbPreset = AudioReverbPreset.User;
                rv.dryLevel = 0f;
                rv.room = 0f;
                rv.roomHF = -300f;
                rv.decayTime = DangerReverbDecay;
                rv.decayHFRatio = 0.9f;
                rv.reflectionsLevel = -600f;
                rv.reflectionsDelay = 0.01f;
                rv.reverbLevel = DangerReverbLevel;
                rv.reverbDelay = 0.03f;
                rv.diffusion = 100f;
                rv.density = 100f;
                }
                src.Play();
                GameManager.instance.StartCoroutine(DestroyAfterRealtime(g, clip.length / DangerSoundPitch + DangerReverbDecay + 0.5f));
            }
            catch (System.Exception) {  }
        }
        private float _dangerSoundAt = -10f;
        // Mods\ParryMod\Uyari\uyari.wav varsa uyarı sesi o (16 bit PCM wav).
        private AudioClip _dangerSound; private bool _dangerSoundTried;
        private AudioClip DangerSound()
        {
            if (_dangerSoundTried) return _dangerSound;
            _dangerSoundTried = true;
            try
            {
                string path = ParryAssets.File("ShinobiCharm", "uyari.wav");
                if (System.IO.File.Exists(path)) { _dangerSound = ParryVisuals.Instance(this).LoadWavPublic(path);  }
            }
            catch (System.Exception) {  }
            return _dangerSound;
        }
        private static readonly System.Collections.Generic.Dictionary<string, AudioClip> _clipByName = new System.Collections.Generic.Dictionary<string, AudioClip>();
        private static AudioClip FindClipByName(string name)
        {
            AudioClip c;
            if (_clipByName.TryGetValue(name, out c) && c != null) return c;
            foreach (AudioClip a in Resources.FindObjectsOfTypeAll<AudioClip>())
                if (a != null && a.name == name) { _clipByName[name] = a; return a; }
            return null;
        }





        // Void geçişinin animasyon sırası ve bütün karelerin ortak çerçevesi (PV'nin yerel biriminde).
        // F3 dışa aktarması da oyundaki çizim de bunu kullanır: ikisi her zaman aynı çerçeve.
        private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, tk2dSpriteAnimationClip>> TransitionSeq(
            PlayMakerFSM m, tk2dSpriteAnimator anim, tk2dSpriteCollectionData col, out float x0, out float y0, out float x1, out float y1, out float ppu)
        {
            var seq = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, tk2dSpriteAnimationClip>>();
            System.Action<string, string> addClip = (label, clipName) =>
            {
                if (clipName == null) return;
                tk2dSpriteAnimationClip c = anim.Library.GetClipByName(clipName);
                if (c != null) seq.Add(new System.Collections.Generic.KeyValuePair<string, tk2dSpriteAnimationClip>(label, c));
            };
            addClip("1_geri_sicrama_hazirlik", GetStateClip(m, "Evade Antic"));
            addClip("2_geri_sicrama", GetStateClip(m, "Evade"));
            addClip("3_sicrama_bitis", GetStateClip(m, "EvadeRecover"));
            tk2dSpriteAnimationClip pose = MakePoseClip(col);
            if (pose != null) seq.Add(new System.Collections.Generic.KeyValuePair<string, tk2dSpriteAnimationClip>("4_poz", pose));
            addClip("5_bekleme", GetStateClip(m, "Idle") ?? "Idle");
            addClip("6_bagirma_hazirlik", "Roar Antic");
            addClip("7_bagirma", GetStateClip(m, "Intro Roar"));
            addClip("8_toparlanma", "Recover");
            x0 = float.MaxValue; y0 = float.MaxValue; x1 = float.MinValue; y1 = float.MinValue; ppu = 64f;
            foreach (var kv in seq)
                foreach (var fr in kv.Value.frames)
                {
                    tk2dSpriteDefinition d = fr.spriteCollection.spriteDefinitions[fr.spriteId];
                    foreach (Vector3 v in d.positions) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); }
                    if (d.texelSize.x > 0f) ppu = 1f / d.texelSize.x;
                }
            x0 -= 0.2f; y0 -= 0.2f; x1 += 0.2f; y1 += 0.2f;
            return seq;
        }

        // ---- Senin çizdiğin bağırma (maske dönüşümü) kareleri: Mods\ParryMod\VoidGecis\bagirma_01.png ... ----
        private static readonly System.Collections.Generic.List<Texture2D> _roarFrames = new System.Collections.Generic.List<Texture2D>();
        private void LoadRoarFrames()
        {
            _roarFrames.Clear();
            string dir = ParryAssets.Dir("PureVessel");
            for (int i = 1; i <= 30; i++)
            {
                string path = System.IO.Path.Combine(dir, "bagirma_" + i.ToString("00") + ".png");
                if (!System.IO.File.Exists(path)) break;
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                _roarFrames.Add(t);
            }
        }

        // Bağırma boyunca senin karelerin sırayla, aralarında yumuşak geçişle oynar (A seçeneği); en sonda son kare biraz kalır.
        private const float RoarFramesHold = 0.15f;   // bağırmanın son %15'i: son kare (simsiyah maske) sabit
        private class RoarFramesPlayer
        {
            public GameObject a, b; public SpriteRenderer ra, rb; public Sprite[] sprites; public MeshRenderer hidden;
        }
        private RoarFramesPlayer StartRoarFrames(GameObject pv, PlayMakerFSM m, tk2dSpriteAnimator anim, tk2dSprite psp)
        {
            if (_roarFrames.Count == 0) return null;
            float x0, y0, x1, y1, ppu;
            TransitionSeq(m, anim, psp.Collection, out x0, out y0, out x1, out y1, out ppu);
            int W = Mathf.CeilToInt((x1 - x0) * ppu), H = Mathf.CeilToInt((y1 - y0) * ppu);
            if (_roarFrames[0].width != W || _roarFrames[0].height != H)
                { }
            var p = new RoarFramesPlayer();
            p.sprites = new Sprite[_roarFrames.Count];
            for (int i = 0; i < _roarFrames.Count; i++)
                p.sprites[i] = Sprite.Create(_roarFrames[i], new Rect(0, 0, _roarFrames[i].width, _roarFrames[i].height), new Vector2(0.5f, 0.5f), ppu);
            MeshRenderer mr = pv.GetComponent<MeshRenderer>();
            Vector3 center = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, -0.002f);
            System.Func<string, int, SpriteRenderer> make = (n, add) =>
            {
                GameObject g = new GameObject(n);
                g.transform.SetParent(pv.transform, false);
                g.transform.localPosition = center + new Vector3(0f, 0f, -0.0005f * add);
                SpriteRenderer r = g.AddComponent<SpriteRenderer>();
                r.sprite = p.sprites[0];
                if (mr != null) { r.sortingLayerID = mr.sortingLayerID; r.sortingOrder = mr.sortingOrder + add; }
                return r;
            };
            p.ra = make("PV Roar Frame A", 1); p.rb = make("PV Roar Frame B", 2);
            p.a = p.ra.gameObject; p.b = p.rb.gameObject;
            p.rb.color = new Color(1f, 1f, 1f, 0f);
            if (mr != null) { mr.enabled = false; p.hidden = mr; }
            return p;
        }
        private static void TickRoarFrames(RoarFramesPlayer p, float k)
        {
            if (p == null || p.sprites.Length == 0) return;
            int n = p.sprites.Length;
            float pos = Mathf.Clamp01(k / (1f - RoarFramesHold)) * (n - 1);
            int i = Mathf.Min(n - 1, Mathf.FloorToInt(pos));
            int j = Mathf.Min(n - 1, i + 1);
            float f = pos - i;
            p.ra.sprite = p.sprites[i];
            p.rb.sprite = p.sprites[j];
            p.rb.color = new Color(1f, 1f, 1f, j == i ? 0f : f * f * (3f - 2f * f));
        }
        private static void StopRoarFrames(RoarFramesPlayer p)
        {
            if (p == null) return;
            if (p.hidden != null) p.hidden.enabled = true;
            if (p.a != null) UnityEngine.Object.Destroy(p.a);
            if (p.b != null) UnityEngine.Object.Destroy(p.b);
        }




        // Uyarı sistemi bossun BÜTÜN durum makinelerini izler (Dung Defender, Nosk gibi ana listede olmayanlar da).
        private readonly System.Collections.Generic.List<PlayMakerFSM> _dangerFsms = new System.Collections.Generic.List<PlayMakerFSM>();
        private float _dangerRefreshAt;
        private readonly System.Collections.Generic.Dictionary<GameObject, float> _dangerCooldown = new System.Collections.Generic.Dictionary<GameObject, float>();
        private readonly System.Collections.Generic.Dictionary<PlayMakerFSM, string> _dangerLastState = new System.Collections.Generic.Dictionary<PlayMakerFSM, string>();
        private const float DangerCooldown = 0.7f;   // aynı boss için iki uyarı arası en az süre (bir saldırıda tek uyarı)

        private void RefreshDangerFsms()
        {
            _dangerFsms.RemoveAll(x => x == null);
            foreach (HealthManager hm in UnityEngine.Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || !hm.isActiveAndEnabled) continue;
                if (!IsBoss(hm))
                {
                    // Normal düşmanlar: sadece uyarısı olanların durum makineleri izlenir; ölünce patlayanlar ayrıca.
                    string nb = BaseName(hm.gameObject.name);
                    if (System.Array.IndexOf(NormalWarnEnemies, nb) >= 0)
                        foreach (PlayMakerFSM f in hm.GetComponents<PlayMakerFSM>())
                            if (f != null && !_dangerFsms.Contains(f)) _dangerFsms.Add(f);
                    continue;
                }
                if (ShinobiOn && HeroController.instance != null && !_fightStartWarned.Contains(hm.GetInstanceID()))
                {
                    string low = hm.gameObject.name.ToLowerInvariant();
                    foreach (string n in FightStartWarnNames)
                        if (low.Contains(n)) { _fightStartWarned.Add(hm.GetInstanceID()); ShowDanger(HeroController.instance, "dövüş başı: " + hm.gameObject.name); break; }
                }
                if (hm.gameObject.name.StartsWith("Grey Prince") && hm.hp > 0) _gpzSeenUntil = Time.time + 2f;
                foreach (PlayMakerFSM f in hm.GetComponents<PlayMakerFSM>())
                    if (f != null && !_dangerFsms.Contains(f)) _dangerFsms.Add(f);
            }
        }

        private void ShinobiDangerTick(HeroController hero)
        {
            if (hero == null) return;
            if (Time.time >= _dangerRefreshAt) { _dangerRefreshAt = Time.time + 1f; RefreshDangerFsms(); }
            // Durumlara giriş artık oyunun kendi içinden yakalanıyor (aynı karede geçilen adımlar da kaçmıyor).
            foreach (PlayMakerFSM c in _dangerFsms) InstallEntryHooks(c);
            if (Time.time >= _hazardScanAt && _dangerFsms.Count > 0 && ShinobiOn) { _hazardScanAt = Time.time + 0.15f; ScanHazards(hero); }
        }

        // Her durumun başına küçük bir çağrı eklenir: o duruma girildiği an (tek karelik adımlar dahil) haber verir.
        private readonly System.Collections.Generic.HashSet<PlayMakerFSM> _entryHooked = new System.Collections.Generic.HashSet<PlayMakerFSM>();
        private void InstallEntryHooks(PlayMakerFSM f)
        {
            if (f == null || !_entryHooked.Add(f)) return;
            try
            {
                foreach (var st in f.FsmStates)
                {
                    string nm = st.Name;
                    PlayMakerFSM ff = f;
                    SFCore.Utils.FsmUtil.InsertMethod(f, nm, 0, () => OnDangerEnter(ff, nm));
                }
            }
            catch (System.Exception) {  }
        }

        private void OnDangerEnter(PlayMakerFSM c, string st)
        {
            try
            {
                if (c == null) return;
                RecordBossState(c, st);
                string bn = c.gameObject.name;
                if (st == "Set Unalert") _warnedOnce.Remove(c.gameObject.GetInstanceID());   // Great Hopper bizi kaybetti: tekrar görünce yine uyarı
                // Husk Guard: zıplayıp yere inince yerden giden dalga (bir süre bossa bağlı olmayan vuruşlar parry'lenmez)
                if (st == "Land" && BaseName(bn) == "zombie guard") _guardStompUntil = Time.time + GuardWaveTime;
                // Normal düşman uzaktaysa (ekran dışı) uyarı yok
                HealthManager chm = c.GetComponent<HealthManager>();
                if (chm != null && !IsBoss(chm) && HeroController.instance != null
                    && Vector2.Distance(c.transform.position, HeroController.instance.transform.position) > NormalWarnRange) return;
                // 1) Bossun kendisinin parry'lenemediği saldırılar: o adımlar dizisine ilk girişte
                bool danger = MatchRule(UnparryableBossRules, bn, st);
                bool prev;
                _dangerPrev.TryGetValue(c, out prev);
                _dangerPrev[c] = danger;
                if (!ShinobiOn) return;
                // 2) Saldırının başladığı belli olan adım (odaklanma, havada belirme, bir önceki parçanın sonu...)
                bool start = MatchStartRule(c, bn, st) || (st == "Roar" && IsFlukemunga(c.gameObject));
                HeroController hero = HeroController.instance;
                // Grey Prince Zote art arda zıplarken her zıplayışta uyarı (aradaki bekleme süresi engellemesin)
                if (start && bn.StartsWith("Grey Prince") && (st == "Jump Antic" || st == "JS Antic")) _dangerCooldown.Remove(c.gameObject);
                // Great Hopper: düşman başına sadece bir kez
                if (start && st == "Set Alert" && !_warnedOnce.Add(c.gameObject.GetInstanceID())) start = false;
                if (hero != null && ((danger && !prev) || start)) TryWarn(hero, c.gameObject, bn + " '" + st + "'");
            }
            catch (System.Exception) { }
        }

        // "boss|Durum" ya da "boss|Durum<Önceki": ikincisi sadece o önceki durumdan gelindiyse (ör. Uumuu'nun
        // seni takip eden elektriği art arda çakar ama sadece saldırının ilk çakışında uyarı).
        private bool MatchStartRule(PlayMakerFSM c, string bossName, string st)
        {
            string low = Canon(bossName);
            foreach (string r in DangerStartRules)
            {
                int bar = r.IndexOf('|');
                if (bar < 0 || !low.Contains(r.Substring(0, bar))) continue;
                string rest = r.Substring(bar + 1);
                int lt = rest.IndexOf('<');
                string state = lt < 0 ? rest : rest.Substring(0, lt);
                if (state != st) continue;
                if (lt < 0) return true;
                if (PrevState(c) == rest.Substring(lt + 1)) return true;
            }
            return false;
        }
        private string PrevState(PlayMakerFSM c)
        {
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>> h;
            if (!_stateHist.TryGetValue(c, out h) || h.Count < 2) return null;
            return h[h.Count - 2].Key;
        }

        private readonly System.Collections.Generic.HashSet<int> _warnedOnce = new System.Collections.Generic.HashSet<int>();
        private void TryWarn(HeroController hero, GameObject src, string what)
        {
            float until;
            if (src != null && _dangerCooldown.TryGetValue(src, out until) && Time.time < until) return;
            if (src != null) _dangerCooldown[src] = Time.time + DangerCooldown;
            ShowDanger(hero, what);
        }

        // Saldırının başladığı belli olduğu adımlar (bu adıma girince bir kez uyarı).
        private static readonly string[] DangerStartRules =
        {
            // Grimm: yerden diken (Nightmare King doğrudan 'Spike Attack'la başlıyor), alev sütunu
            "grimm|Spike Tele In", "grimm|Spike Attack", "grimm|Pillar Tele In",
            // Pure Vessel: dalıştan sonra yerden çıkan dikenler (parry'lenince gelen 2. dalga da)
            "hk prime|Plume Pause",
            // Mato: odaklanma (hep dönme saldırısına gidiyor). Oro'nunki dash'e de gidebildiği için yok.
            "mato|NA Charge",
            // Sly: 3'lü saldırının 2. parçasının sonu (3. parça dönerek yukarı çıkış, parry'lenmez),
            // 2. fazda her dönerek üstüne gelişten önce
            "sly|BSlash S3", "sly|Rage Slash Antic",
            // Soul Master / Soul Tyrant: aşağı dalış için havada belirdiğinde
            "mage lord|Quake Antic",
            // Nosk: koşmaya başlarken
            "mimic spider|Charge Init",
            // Dung Defender / White Defender: dışkıya girip çıkma, yunus gibi dalma, top olup zıplama, yere vurma
            "defender|Timer", "defender|Dolph Dir", "defender|RJ Antic", "defender|G Slam Antic", "defender|Air Dive Antic",
            // False Knight ve Failed Champion (adları "False Knight New" / "False Knight Dream"):
            // gürzü yere vurup şok dalgası gönderdiği saldırı: her vuruşun hemen öncesinde (gürzü kaldırdığı an)
            "false knight|S Attack Antic",
            // Marmu: her top olup üstüne gelişinden önce (durup yeniden başladığında da)
            "marmu|Antic",
            // Uumuu: elektrik saldırıları (peşinden gelen çarpma ve çoklu elektrik)
            "jellyfish|Zapping<Choice", "jellyfish|Pattern Choice",
            // Vengefly King: üstüne dalış
            "giant buzzer|Swoop Antic", "big buzzer|Swoop Antic",
            // Crystal Guardian (ve öfkeli hali): ışın, zıplayıp üstüne inme, tavandan ışınlar
            "beam miner|Shoot Antic", "beam miner|Shoot Antic 2", "beam miner|Roar Start",
            // Radiance / Absolute Radiance: her saldırının başı
            "radiance|Nail Top Sweep", "radiance|Nail L Sweep", "radiance|Nail R Sweep",
            "radiance|Nail Top Sweep 2", "radiance|Nail L Sweep 2", "radiance|Nail R Sweep 2",
            "radiance|Eye Beam Wait", "radiance|Beam Sweep L", "radiance|Beam Sweep R",
            "radiance|Beam Sweep L 2", "radiance|Beam Sweep R 2", "radiance|Nail Fan Wait", "radiance|Orb Wait",
            // Normal düşmanlar: Husk Guard zıplayıp yere vurma (dalga), Fool Eater ısırma, Volatile Mosskin patlama, Violent Husk koşu
            "zombie guard|Stomp Antic", "plant trap|Ready", "mossman_shaker|Attack Antic", "bursting zombie|Antic",
            // Grey Prince Zote: zıplama (her zıplayış, inince dalga), yukarıdan düşme (zeminden geçip üstten iniş,
            // hücumda duvardan geçip üstten iniş), balon çağırma
            "ceiling dropper|Startle", "zombie beam miner|Beam Antic", "flukemunga|Roar",
            "fungus flyer|Spit Antic", "mushroom turret|Attack Antic", "zombie fungus|Attack Antic",
            "electric mage|Zap Antic",                         // Volt Twister elektrik (parry'lenmez)
            "flamebearer|Pillar Antic",                        // Grimmkin Nightmare yerden alev (parry'lenmez)
            "grey prince|Jump Antic", "grey prince|JS Antic", "grey prince|CT In L", "grey prince|CT In R", "grey prince|B Roar Antic"
        };

        // Belirgin saldırı başlangıcı olmayan bosslar: dövüş başında bir kez uyarı.
        private static readonly string[] FightStartWarnNames = { "no eyes" };
        private readonly System.Collections.Generic.HashSet<int> _fightStartWarned = new System.Collections.Generic.HashSet<int>();

        // Belirdiği an uyarı verilen parry'lenemez nesneler (her biri için bir kez).
        private static readonly string[] AppearWarnNames = { "galien mini hammer" };
        private readonly System.Collections.Generic.HashSet<int> _activeHazards = new System.Collections.Generic.HashSet<int>();
        private readonly System.Collections.Generic.HashSet<int> _activeNow = new System.Collections.Generic.HashSet<int>();

        // ---- Öğrenme kaydı: parry'lenemez bir şey belirince / vurunca bossun son durumlarını yazar.
        // Bu kayıtlarla her saldırının BAŞLADIĞI adımı uyarı listesine ekleyeceğiz.
        private readonly System.Collections.Generic.Dictionary<PlayMakerFSM, System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>> _stateHist
            = new System.Collections.Generic.Dictionary<PlayMakerFSM, System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>>();
        private readonly System.Collections.Generic.HashSet<int> _seenHazards = new System.Collections.Generic.HashSet<int>();
        private float _hazardScanAt;

        private void RecordBossState(PlayMakerFSM c, string st)
        {
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>> h;
            if (!_stateHist.TryGetValue(c, out h)) { h = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>(); _stateHist[c] = h; }
            if (h.Count > 0 && h[h.Count - 1].Key == st) return;
            h.Add(new System.Collections.Generic.KeyValuePair<string, float>(st, Time.time));
            if (h.Count > 8) h.RemoveAt(0);
        }


        private static string NameChain(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent) sb.Append(" <- ").Append(p.name);
            return sb.ToString();
        }

        private void ScanHazards(HeroController hero)
        {
            _activeNow.Clear();
            bool warned = false;
            foreach (DamageHero d in UnityEngine.Object.FindObjectsOfType<DamageHero>())
            {
                if (d == null || !d.isActiveAndEnabled) continue;
                int id = d.GetInstanceID();
                _activeNow.Add(id);
                if (_activeHazards.Contains(id)) continue;   // zaten açıktı
                string chain = NameChain(d.transform);
                string low = chain.ToLowerInvariant();
                if (ShinobiOn && !warned)
                    foreach (string n in AppearWarnNames)
                        if (low.Contains(n)) { warned = true; ShowDanger(hero, "belirdi: " + chain); break; }
            }
            _activeHazards.Clear();
            foreach (int id in _activeNow) _activeHazards.Add(id);
        }



        // Uyarı efekti: Fury of the Fallen'ın patlamasının küçük bir kopyası, Knight'ın başının hemen üstünde,
        // biraz yavaş oynar ve çabuk solar.
        private const float DangerScale = 0.8f;      // oyundaki 2,1 (küçük sayı = küçük efekt)
        private const float DangerHeight = 1.25f;    // Knight'ın merkezinden yukarı
        private const float DangerSpeed = 0.7f;      // animasyon hızı (1 = normal)
        private const float DangerLife = 0.55f;      // toplam süre (sn)
        private const float DangerFadeStart = 0.25f; // bu andan sonra solmaya başlar

        private void ShowDanger(HeroController hero, string what)
        {
            if (_rageBurst == null)
            {
                Transform t = hero.transform.Find("Charm Effects/Rage Burst Effect");
                if (t == null) {  return; }
                _rageBurst = t.gameObject;
            }
            GameObject fx = UnityEngine.Object.Instantiate(_rageBurst, hero.transform);
            fx.name = "Shinobi Danger (ParryMod)";
            foreach (PlayMakerFSM f in fx.GetComponents<PlayMakerFSM>()) UnityEngine.Object.Destroy(f);   // kendini 0,5 sn'de kapatmasın
            fx.transform.localPosition = new Vector3(0f, DangerHeight, -1f);
            fx.transform.localScale = Vector3.one * DangerScale;
            Animator an = fx.GetComponent<Animator>();
            if (an != null) an.speed = DangerSpeed;
            DangerFade df = fx.AddComponent<DangerFade>();
            df.Life = DangerLife; df.FadeStart = DangerFadeStart;
            fx.SetActive(true);
            // Uyarı sesi: Mods\ParryMod\Uyari\uyari.wav varsa o, yoksa Carefree Melody'nin metal tınısı.
            AudioClip snd = DangerSound() ?? FindClipByName(DangerSoundName);
            if (snd != null && Time.unscaledTime - _dangerSoundAt > 0.1f) { _dangerSoundAt = Time.unscaledTime; PlayDangerSound(snd); }
        }

        private string OnLanguageGet(string key, string sheet, string orig)
        {
            if (_shinobiId > 0 && key != null)
            {
                if (key == "CHARM_NAME_" + _shinobiId) return ShinobiName;
                if (key == "CHARM_DESC_" + _shinobiId) return ShinobiDesc;
            }
            if (orig != null && key != null && key.ToUpperInvariant().Contains("DREAMNAIL")
                && orig.Contains("fully awoken") && !orig.Contains(DreamNailDescExtra))
            {
                string sep = orig.Contains("<br>") ? "<br><br>" : "\n\n";
                return orig + sep + DreamNailDescExtra;
            }
            return orig;
        }


        private static bool WielderEquipped()
        {
            try { return PlayerData.instance.GetBool("equippedCharm_30"); } catch { return false; }   // Dream Wielder
        }

        private void OnHeroAttack(On.HeroController.orig_Attack orig, HeroController self, GlobalEnums.AttackDirection dir)
        {
            orig(self, dir);
            if (Time.time >= _dreamBuffUntil) return;
            try
            {
                Vector2 d = dir == GlobalEnums.AttackDirection.upward ? Vector2.up
                          : dir == GlobalEnums.AttackDirection.downward ? Vector2.down
                          : (self.cState.facingRight ? Vector2.right : Vector2.left);
                int count = UnityEngine.Random.value < 0.5f ? 2 : 3;
                if (WielderEquipped()) count += UnityEngine.Random.value < 0.5f ? 1 : 2;   // Dream Wielder: 1-2 öz fazla
                for (int i = 0; i < count; i++) GameManager.instance.StartCoroutine(EssenceBubble(self, d, i));
            }
            catch (System.Exception e) { Log("HATA (rüya baloncuğu): " + e.Message); }
        }

        private IEnumerator EssenceBubble(HeroController hc, Vector2 d, int i)
        {
            yield return new WaitForSeconds(0.03f * i);   // hafif sıralı çıksınlar
            if (hc == null) yield break;
            GameObject b = new GameObject("Dream Essence Bubble (ParryMod)");
            SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = EssenceSprite();
            Renderer hr = hc.GetComponent<Renderer>();
            if (hr != null) { sr.sortingLayerID = hr.sortingLayerID; sr.sortingOrder = hr.sortingOrder + 5; }
            Vector2 side = new Vector2(-d.y, d.x);
            // Ayrık çıksınlar: her biri yana doğru farklı bir şeritte.
            float lane = ((i % 2 == 0 ? 1 : -1) * ((i + 1) / 2)) * 0.7f + UnityEngine.Random.Range(-0.2f, 0.2f);   // 0, -1, +1, -2, +2 ... şeritleri
            Vector3 p0 = hc.transform.position + (Vector3)(d * (1.2f + 0.3f * i) + side * lane);
            p0.z = hc.transform.position.z - 0.05f;
            b.transform.position = p0;
            float size = UnityEngine.Random.Range(0.49f, 0.7f);   // oyundaki rüya özlerinden %30 küçük
            Vector2 v = d * UnityEngine.Random.Range(5.2f, 8.5f) + side * (lane * 2.2f + UnityEngine.Random.Range(-0.6f, 0.6f));   // %30 daha hızlı, birbirinden açılarak
            float life = UnityEngine.Random.Range(1.0f, 1.3f), t0 = Time.time;
            while (b != null && Time.time - t0 < life)
            {
                float k = (Time.time - t0) / life;
                v *= Mathf.Pow(0.15f, Time.deltaTime);          // yavaşlar ama daha uzağa gider
                v += Vector2.up * 2.2f * Time.deltaTime;         // yavaşça yukarı süzülür
                b.transform.position += (Vector3)(v * Time.deltaTime);
                float sIn = Mathf.Clamp01(k / 0.15f);
                b.transform.localScale = Vector3.one * size * (0.6f + 0.4f * sIn) * (1f + 0.06f * Mathf.Sin(k * 10f + i));
                b.transform.Rotate(0f, 0f, 40f * Time.deltaTime * (i % 2 == 0 ? 1f : -1f));   // desen yavaşça döner
                Color c = EssenceColor; c.a = Mathf.Clamp01(k / 0.1f) * (1f - k * k);
                sr.color = c;
                yield return null;
            }
            if (b != null) UnityEngine.Object.Destroy(b);
        }

        private void DreamGlowTick(HeroController hc)
        {
            float now = Time.time;
            bool active = now < _dreamBuffUntil;
            if (!active && !_slashTinted) return;
            if (_slashSprites == null)
            {
                _slashSprites = new System.Collections.Generic.List<tk2dSprite>();
                _slashOrig = new System.Collections.Generic.List<Color>();
                foreach (var f in typeof(HeroController).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    if (f.FieldType != typeof(NailSlash)) continue;
                    NailSlash ns = f.GetValue(hc) as NailSlash;
                    if (ns == null) continue;
                    tk2dSprite sp = ns.GetComponent<tk2dSprite>();
                    if (sp == null || _slashSprites.Contains(sp)) continue;
                    _slashSprites.Add(sp); _slashOrig.Add(sp.color);
                }
            }
            float k = 0f;
            if (active)
            {
                float fin = Mathf.Clamp01((now - _dreamBuffStart) / DreamGlowFadeIn);
                float fout = Mathf.Clamp01((_dreamBuffUntil - now) / DreamGlowFadeOut);
                k = Mathf.Min(fin, fout);
                k = k * k * (3f - 2f * k);                                   // yumuşak geçiş
                k *= 0.85f + 0.15f * Mathf.Sin(now * 6f);                    // hafif nabız gibi parıltı
            }
            for (int i = 0; i < _slashSprites.Count; i++)
            {
                tk2dSprite sp = _slashSprites[i];
                if (sp == null) continue;
                sp.color = Color.Lerp(_slashOrig[i], DreamNailGlow, k);
            }
            _slashTinted = active;
        }

        private void OnDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            orig(self);
            try
            {
                if (!PlayerData.instance.GetBool("dreamNailUpgraded")) {  return; }
                string n = self.gameObject.name.ToLowerInvariant();
                HealthManager hmr = self.GetComponentInParent<HealthManager>();
                if (hmr != null) n += " " + hmr.gameObject.name.ToLowerInvariant();
                foreach (string ex in DreamBuffExcluded)
                    if (n.Contains(ex)) {  return; }
                // Ruh vermeyen düşman = rüya iğnesi işe yaramıyor.
                var ns = typeof(EnemyDreamnailReaction).GetField("noSoul", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (ns != null && System.Convert.ToBoolean(ns.GetValue(self))) {  return; }
                if (Time.time >= _dreamBuffUntil) _dreamBuffStart = Time.time;   // yeni başlıyorsa parlama baştan açılır
                float dur = WielderEquipped() ? DreamNailBuffTimeWielder : DreamNailBuffTime;
                _dreamBuffUntil = Time.time + dur;
            }
            catch (System.Exception e) { Log("HATA (rüya iğnesi): " + e.Message); }
        }

        // ---------- Parry ile sersemleme ----------
        // Parry bossu sersemletirse: bossa özel parry iptalleri/geri kaçmalar yapılmaz (sersemleme kazanır)
        // ve sersemleme süresince boss 1.5 kat hasar alır. Sersemleyebilen bütün bosslar için geçerli.
        private const float StunDamageMul = 1.5f;
        private GameObject _parryStunBoss;
        private float _parryStunTime = -10f;
        private GameObject _stunBonusBoss;
        private bool _stunBonusActive;
        private int _stunBonusToken;

        // ---------- Grimm: matkap dalışı parry'si -> anında sersemleme, sonra 5 hızlı saldırı ----------
        private const float GrimmFrenzyMulNightmare = 1.3f;   // Nightmare King Grimm: sonraki 5 saldırı %30 hızlı
        private const float GrimmFrenzyMulNormal = 1.2f;      // normal Grimm: %20 hızlı
        private const int GrimmFrenzyAttacks = 5;
        private int _grimmTempMods;                           // başka geçici Grimm hızlandırmaları (ceza yarasası, pusu) sürüyor mu
        private int _grimmFrenzyToken;
        // Hız çarpanı uygulanan hız değişkenleri (geri kayma ve yarasa hızı başka sistemlerce değiştirildiği için hariç).
        private static readonly string[] GrimmSpeedVars = { "AD Speed", "G Dash Speed", "Slash Speed", "Uppercut X Speed" };

        // Balon saldırısının alevleri de hızlansın (log'daki teşhise göre):
        //  - yana giden toplar: SetVelocity2d 'x' (yatay hız) x mul, FireGrimmBall 'force' x mul² (aynı yol, daha kısa sürede)
        //  - aşağı düşen alevler: FlingObjectsFromGlobalPool 'speedMin/speedMax' x mul
        // Değişkene bağlı (UseVariable) değerlere dokunulmaz; değiştirilenler geri yükleme listesine eklenir.
        private static readonly string[] GrimmBalloonFireStates =
        {
            "Fire Low R", "Fire Mid R", "Fire High R", "Fire Low L", "Fire Mid L", "Fire High L", "Alt", "Down"
        };
        private static int ScaleGrimmBalloonShots(PlayMakerFSM control,  float mul,
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> restore)
        {
            int n = 0;
            var seen = new System.Collections.Generic.HashSet<HutongGames.PlayMaker.FsmFloat>();
            foreach (var st in control.FsmStates)
            {
                if (System.Array.IndexOf(GrimmBalloonFireStates, st.Name) < 0 || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null) continue;
                    string tn = a.GetType().Name;
                    string[] fields; float k;
                    if (tn == "SetVelocity2d") { fields = new[] { "x", "y" }; k = mul; }
                    else if (tn == "FireGrimmBall") { fields = new[] { "force" }; k = mul * mul; }
                    else if (tn == "FlingObjectsFromGlobalPool") { fields = new[] { "speedMin", "speedMax" }; k = mul; }
                    else continue;
                    foreach (string fn in fields)
                    {
                        var fi = a.GetType().GetField(fn);
                        var f = fi != null ? fi.GetValue(a) as HutongGames.PlayMaker.FsmFloat : null;
                        if (f == null || f.UseVariable || !seen.Add(f)) continue;
                        restore.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>(f, f.Value));
                        f.Value *= k;
                        n++;
                    }
                }
            }
            return n;
        }

        private void GrimmDrillStun(GameObject boss)
        {
            try
            {
                PlayMakerFSM stun = null;
                foreach (PlayMakerFSM f in boss.GetComponents<PlayMakerFSM>()) if (f.FsmName == "Stun") { stun = f; break; }
                string before = stun != null ? stun.ActiveStateName : "yok";
                MarkParryStun(boss);   // stun kuralı: iptaller yapılmaz, sersemken hasar x1.5
                PlayMakerFSM ctl0 = MainFsm(boss);
                if (GrimmStunned(boss) || (ctl0 != null && ctl0.ActiveStateName.StartsWith("Stun")))
                    { }
                else if (stun != null && before != "Stop" && before != "Reset" && HasState(stun, "Stun"))
                {
                    stun.SetState("Stun");   // oyunun kendi sersemlemesi (sayaç sıfırlanır, yarasalara dağılır)
                }
                else
                {
                    PlayMakerFSM control = MainFsm(boss);
                    if (control != null) control.SendEvent("STUN");
                }
                float mul = boss.name.ToLowerInvariant().Contains("nightmare") ? GrimmFrenzyMulNightmare : GrimmFrenzyMulNormal;
                GameManager.instance.StartCoroutine(GrimmFrenzyRoutine(boss, mul, ++_grimmFrenzyToken));
            }
            catch (System.Exception e) { Log("HATA (Grimm dalış sersemlemesi): " + e); }
        }

        // Grimm'in hızlanmasını kaydeden ve sahne kapanırsa (boss yok olursa) geri alan parça.
        private class GrimmFrenzyState : MonoBehaviour
        {
            public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>> clips;
            public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> floats;
            public void RestoreAll()
            {
                if (clips != null) foreach (var kv in clips) if (kv.Key != null) kv.Key.fps = kv.Value;
                if (floats != null) foreach (var kv in floats) if (kv.Key != null) kv.Key.Value = kv.Value;
                clips = null; floats = null;
            }
            void OnDestroy() { RestoreAll(); }
        }

        private IEnumerator GrimmFrenzyRoutine(GameObject boss, float mul, int token)
        {
            PlayMakerFSM control = MainFsm(boss);
            if (control == null) {  yield break; }
            // 1) Sersemlemenin başlamasını, sonra yarasalardan yeniden toplanmasını bekle.
            float t0 = Time.time;
            while (boss != null && !GrimmStunned(boss) && !control.ActiveStateName.StartsWith("Stun") && !control.ActiveStateName.StartsWith("Explode")
                   && Time.time - t0 < 1f) yield return null;
            t0 = Time.time;
            bool reformed = false;
            while (boss != null && token == _grimmFrenzyToken && Time.time - t0 < 12f)
            {
                string st = control.ActiveStateName;
                if (st == "Reformed" || st == "Adjust HP") reformed = true;
                else if (reformed) break;   // toplandı, yeni saldırısına geçiyor
                yield return null;
            }
            if (boss == null || token != _grimmFrenzyToken) yield break;
            if (!reformed) {  yield break; }
            // 2) Başka geçici hızlandırma sürüyorsa bitmesini bekle (değerler karışmasın).
            t0 = Time.time;
            while (_grimmTempMods > 0 && Time.time - t0 < 3f) yield return null;

            // 3) Hızlandır: animasyonlar, bekleme süreleri, hareket hızları.
            GrimmFrenzyState fs = boss.GetComponent<GrimmFrenzyState>();
            if (fs == null) fs = boss.AddComponent<GrimmFrenzyState>();
            fs.RestoreAll();
            fs.clips = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>>();
            tk2dSpriteAnimator an = boss.GetComponent<tk2dSpriteAnimator>();
            if (an != null && an.Library != null)
                foreach (tk2dSpriteAnimationClip c in an.Library.clips)
                {
                    if (c == null) continue;
                    fs.clips.Add(new System.Collections.Generic.KeyValuePair<tk2dSpriteAnimationClip, float>(c, c.fps));
                    c.fps *= mul;
                }
            var names = new System.Collections.Generic.List<string>();
            foreach (var st in control.FsmStates) names.Add(st.Name);
            fs.floats = ScaleWaits(control, names.ToArray(), 1f / mul);
            int waitCount = fs.floats.Count;
            foreach (string v in GrimmSpeedVars)
            {
                var f = control.FsmVariables.FindFsmFloat(v);
                if (f == null) continue;
                fs.floats.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>(f, f.Value));
                f.Value *= mul;
            }
            int ballCount = ScaleGrimmBalloonShots(control, mul, fs.floats);
            // Gözler: senin boyadığın parlayan gözlü resimlere yumuşakça geçilir.
            GrimmGlowSkin eyes = GrimmGlowSkin.For(boss, this);
            if (eyes != null) eyes.target = 1f;

            // 4) 5 saldırı say: her saldırı ışınlanıp kaybolarak (Out Pause) ya da patlayarak (Explode Pause) biter.
            int done = 0;
            string last = control.ActiveStateName;
            while (boss != null && token == _grimmFrenzyToken && done < GrimmFrenzyAttacks)
            {
                string st = control.ActiveStateName;
                if (st != last)
                {
                    if (st == "Out Pause" || st == "Explode Pause") { done++;  }
                    if (st == "Stun Reset" || st == "Death Start") {  break; }
                    last = st;
                }
                yield return null;
            }
            // 5) Normale dön (başka geçici hızlandırma bitince), gözler yavaşça söner.
            t0 = Time.time;
            while (_grimmTempMods > 0 && Time.time - t0 < 3f) yield return null;
            if (fs != null) fs.RestoreAll();
            if (eyes != null) eyes.target = 0f;
        }

        private void MarkParryStun(GameObject boss)
        {
            if (boss == null) return;
            _parryStunBoss = boss;
            _parryStunTime = Time.time;
            GameManager.instance.StartCoroutine(StunBonusRoutine(boss, ++_stunBonusToken));
        }

        private bool SkipCancelForStun(GameObject boss, string what)
        {
            if (boss == null || boss != _parryStunBoss || Time.time - _parryStunTime > 0.5f) return false;
            return true;
        }

        private static bool BossIsStunned(GameObject boss)
        {
            if (boss == null) return false;
            PlayMakerFSM m = MainFsm(boss);
            if (m != null && m.ActiveStateName != null && m.ActiveStateName.Contains("Stun")) return true;
            return GrimmStunned(boss);
        }

        private IEnumerator StunBonusRoutine(GameObject boss, int token)
        {
            // Sersemlemenin başlamasını bekle.
            float t0 = Time.time;
            while (boss != null && !BossIsStunned(boss) && Time.time - t0 < 0.6f) yield return null;
            if (boss == null || token != _stunBonusToken) yield break;
            if (!BossIsStunned(boss))
            {
                PlayMakerFSM mm = MainFsm(boss);
                yield break;
            }
            _stunBonusBoss = boss;
            _stunBonusActive = true;
            PlayMakerFSM m = MainFsm(boss);
            int notStunned = 0;
            float tEnd = Time.time + 15f;
            while (boss != null && token == _stunBonusToken && Time.time < tEnd)
            {
                if (BossIsStunned(boss)) notStunned = 0;
                else if (++notStunned >= 3) break;   // birkaç kare üst üste sersem değilse bitti
                yield return null;
            }
            if (token == _stunBonusToken)
            {
                _stunBonusActive = false;
                _stunBonusBoss = null;
            }
        }

        private static bool GrimmStunned(GameObject boss)
        {
            foreach (PlayMakerFSM f in boss.GetComponents<PlayMakerFSM>())
                if ((f.FsmName == "Stun" || f.FsmName == "Stun Control") && f.ActiveStateName == "Stun") return true;
            return false;
        }

        private HutongGames.PlayMaker.FsmFloat _grimmEvadeRestore;
        private float _grimmEvadeOrig;

        // Grimm'in alev yarasası saldırısının adımları (iki versiyon).
        private static readonly string[] PunishFirebatStates =
        {
            "FB Antic", "FB Cast", "Firebat 1", "Firebat 2", "Firebat 3", "Firebat 4", "Extra Bat", "FB Cast End"
        };

        // Verilen durumlardaki "Wait" (bekle) eylemlerinin süresini çarpar; eski değerleri döndürür.
        private static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>>
            ScaleWaits(PlayMakerFSM fsm, string[] states, float mul)
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>>();
            var seen = new System.Collections.Generic.HashSet<HutongGames.PlayMaker.FsmFloat>();
            foreach (var st in fsm.FsmStates)
            {
                if (System.Array.IndexOf(states, st.Name) < 0 || st.Actions == null) continue;
                foreach (var a in st.Actions)
                {
                    if (a == null || a.GetType().Name != "Wait") continue;
                    var field = a.GetType().GetField("time");
                    var f = field != null ? field.GetValue(a) as HutongGames.PlayMaker.FsmFloat : null;
                    if (f == null || !seen.Add(f)) continue;
                    list.Add(new System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>(f, f.Value));
                    f.Value *= mul;
                }
            }
            return list;
        }

        // Yarasa saldırısı bitince (Grimm ışınlanınca) bekleme sürelerini ve yarasa hızını geri yükler.
        private IEnumerator RestoreFirebats(PlayMakerFSM control,
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<HutongGames.PlayMaker.FsmFloat, float>> waits,
            HutongGames.PlayMaker.FsmFloat batSpeed, float origBat)
        {
            float timeout = Time.time + 5f;
            while (control != null && Time.time < timeout)
            {
                string s = control.ActiveStateName;
                if (s == "Tele Out" || s == "Out Pause" || s == "Stun Reset" || s == "Stun") break;
                yield return null;
            }
            foreach (var kv in waits) kv.Key.Value = kv.Value;
            if (batSpeed != null) batSpeed.Value = origBat;
            _grimmTempMods = Mathf.Max(0, _grimmTempMods - 1);
        }

        // Grimm ışınlanarak kaybolur, beklemeden Knight'ın arkasında belirir ve pençe saldırısı yapar.
        // Haritadaki kendi akışı kullanılır: Tele Out -> (Out Pause atlanır) -> Slash Tele In -> Slash Antic.
        private IEnumerator TeleBehindAndClaw(GameObject boss, PlayMakerFSM control)
        {
            tk2dSpriteAnimator anim = boss.GetComponent<tk2dSpriteAnimator>();
            // Pençe hazırlığını kısalt (pusu bitince geri yüklenir).
            // Normal Grimm'de pusu kendi normal hızında (sadece Nightmare King Grimm'de hızlı).
            bool fastAmbush = boss.name.ToLowerInvariant().Contains("nightmare");
            float ambushAnim = fastAmbush ? AmbushAnimSpeed : 1f;
            var windupWaits = ScaleWaits(control, new[] { "Slash Antic" }, fastAmbush ? AmbushWindupMul : 1f);
            _grimmTempMods++;

            control.SendEvent("TELE OUT");
            yield return null;
            if (anim != null) anim.ClipFps *= ambushAnim; // kaybolma animasyonu hızlı (Nightmare King Grimm)

            float timeout = Time.time + 2f;
            while (boss != null && control != null && control.ActiveStateName != "Out Pause" && Time.time < timeout)
                yield return null;
            if (boss == null || control == null) yield break;
            if (control.ActiveStateName != "Out Pause")
            {
                foreach (var kv in windupWaits) kv.Key.Value = kv.Value;
            _grimmTempMods = Mathf.Max(0, _grimmTempMods - 1);
                yield break;
            }

            // Belireceği yer: Knight'ın baktığı yönün tersi (arkası). Arena dışına taşarsa önü.
            HeroController hero = HeroController.instance;
            float hx = hero.transform.position.x;
            bool facingRight = hero.cState.facingRight;
            float bx = facingRight ? hx - AmbushDistance : hx + AmbushDistance;

            var minX = control.FsmVariables.FindFsmFloat("Min X");
            var maxX = control.FsmVariables.FindFsmFloat("Max X");
            if (minX != null && maxX != null && (bx < minX.Value + 1f || bx > maxX.Value - 1f))
            {
                bx = facingRight ? hx + AmbushDistance : hx - AmbushDistance;
            }

            var teleX = control.FsmVariables.FindFsmFloat("Tele X");
            if (teleX != null) teleX.Value = bx;
            var uppercutRange = control.FsmVariables.FindFsmBool("Uppercut Range");
            if (uppercutRange != null) uppercutRange.Value = false; // yukarı pençe değil, yandan pençe

            control.SetState("Slash Tele In");
            yield return null;
            if (boss == null || control == null) yield break;
            if (anim != null) anim.ClipFps *= ambushAnim; // belirme animasyonu hızlı (Nightmare King Grimm)

            // Belirme bitince pençeyi zorla (normalde bazen yukarı pençe ya da kaçma seçebiliyor).
            timeout = Time.time + 1.5f;
            while (boss != null && control != null && control.ActiveStateName == "Slash Tele In" && Time.time < timeout)
                yield return null;
            if (boss == null || control == null) yield break;
            string s = control.ActiveStateName;
            if (s != "Slash Antic" && s != "Slash 1")
            {
                control.SetState("Slash Antic");
            }

            // Pençe başlayınca hazırlık süresini normale döndür.
            timeout = Time.time + 2f;
            while (control != null && control.ActiveStateName == "Slash Antic" && Time.time < timeout)
                yield return null;
            foreach (var kv in windupWaits) kv.Key.Value = kv.Value;
            _grimmTempMods = Mathf.Max(0, _grimmTempMods - 1);

            GameManager.instance.StartCoroutine(AfterCancelCheck(boss, control));
        }

        // Teşhis: iptalden sonra boss bir yere takılırsa log'dan görelim.
        private IEnumerator AfterCancelCheck(GameObject boss, PlayMakerFSM control)
        {
            yield return new WaitForSeconds(1f);
            if (boss != null && control != null)
                { }
            yield return new WaitForSeconds(2f);
            if (boss != null && control != null)
                { }
        }

        // Elder Hu halkasını parry'leyince aynı halkanın diğer vuruş alanlarının korunma süresi.
        private const float HuRingGrace = 2.0f;
        private const float HuMultiRingGrace = 0.6f; // halka parry'sinden sonra diğer halkalar bu kadar süre vuramaz
        private float _huRingShieldUntil;
        private GameObject _bossGraceRoot;
        private float _bossGraceUntil;
        private const float BossGraceTime = 0.8f;   // parry'den sonra aynı boss'un bu kadar süre vuramaması (dash/dalış devamı)

        private static bool IsUnparryable(string attackerInfo)
        {
            string lower = attackerInfo.ToLowerInvariant();
            // Rüya savaşçılarının (Hu, Gorb, Galien, Markoth, Xero, No Eyes, Marmu) kendi gövdeleri parry'lenemez.
            // Sadece vuruşun kendisi gövdeyse: silahları/mermileri ayrı nesneler, onlar etkilenmez.
            if (lower.StartsWith("ghost warrior")) return true;
            // Broken Vessel'in oyundaki adı "Infected Knight": adındaki "infect" yüzünden bütün saldırıları
            // parry'lenemez sayılıyordu. Onun kendi saldırıları (gövde, dash, kılıç) parry'lenebilir.
            if (lower.Contains("infected knight")) lower = lower.Replace("infected knight", "broken vessel");
            foreach (string part in Unparryable)
            {
                if (lower.Contains(part)) return true;
            }
            return false;
        }
    }

    // =====================================================================
    //  Parry görselleri ve sesleri (kendi çizimlerin + Silksong'dan alınan sesler)
    //  Mod klasörüne konan dosyalar (yoksa o parça atlanır, mod çalışmaya devam eder):
    //    Parry_Prep_1..3.png     : parry tuşuna basınca (sarı halka) - kılıcı çekme
    //    Parry_Success_1..2.png  : başarılı parry (mavi halka)
    //    Parry_Recover_1..2.png  : başka parry yapılmazsa toparlanma
    //    Parry_Effect_1..4.png   : başarılı parry'de kılıcın ucundan çıkan efekt
    //    knight_parry_prepare.wav: parry tuşuna basınca
    //    knight_parry.wav        : başarılı parry'de
    // =====================================================================
    // Uyarı efektini animasyondan SONRA (LateUpdate) soldurur ve süresi dolunca siler.
    internal class DangerFade : MonoBehaviour
    {
        // Kendi çizdiğin uyarı kareleri: oyunun karesi adına göre ('rage_burst_effect000' ...) yerine geçer.
        public static readonly System.Collections.Generic.Dictionary<string, Sprite> Custom = new System.Collections.Generic.Dictionary<string, Sprite>();
        public float Life = 0.55f, FadeStart = 0.25f;
        private float _t;
        private SpriteRenderer _r;
        private float _base = -1f, _last = -1f;
        private void Awake() { _r = GetComponent<SpriteRenderer>(); }
        private void LateUpdate()
        {
            _t += Time.deltaTime;
            if (_t >= Life) { Destroy(gameObject); return; }
            if (_r == null) return;
            Sprite cs;
            if (_r.sprite != null && Custom.TryGetValue(_r.sprite.name, out cs)) _r.sprite = cs;
            Color c = _r.color;
            // Animasyon rengi kendisi değiştirdiyse onu taban al, değiştirmediyse ilk rengi kullan (üst üste çarpılmasın).
            if (_last < 0f || Mathf.Abs(c.a - _last) > 0.001f) _base = c.a;
            float k = _t > FadeStart ? 1f - Mathf.Clamp01((_t - FadeStart) / Mathf.Max(0.01f, Life - FadeStart)) : 1f;
            c.a = _base * k;
            _r.color = c;
            _last = c.a;
        }
    }

    internal class ParryVisuals : MonoBehaviour
    {
        // ---- Ayarlar ----
        // Tüm kareler aynı kurala göre hizalı: ayak tabanı pivotun 53 px altında, kafanın ortası pivotun tam üstünde.
        // Böylece kareler arasında ayaklar ve kafa yerinde kalır, titreme olmaz.
        private const float KnightPPU = 66f;               // ölçek: çizimler oyundaki Knight'tan ~%4 kısaydı (videoda ölçüldü); küçük sayı = büyük çizim
        private const float KnightYOffset = -0.613f;       // ayakları oyundaki Knight'ın ayaklarıyla aynı hizaya getirir (videoda 4 px yukarıdaydı)
        private const float KnightXOffset = -0.023f;       // kafanın ortasını oyundaki Knight'ın kafasının ortasına getirir (videoda 1,6 px kayıktı)
        private const float EffectPPU = 100f;              // efektin ölçeği (bir kez daha ~%33 büyütüldü; küçük sayı = büyük efekt)
        private const bool EffectMirror = false;           // videoda ters çıktı: yönü çevrildi
        // Efektin çıktığı yer: hazırlık pozunda (3. kare) ileri-aşağı uzanan kılıcın ucunun hemen önü (Knight'a göre, birim).
        // Kılıcın ucu çizimde (-0,63, -0,39); 0,25 birim daha önüne konur ve çizimlerin kaydırması eklenir.
        private static readonly Vector2 NailTip = new Vector2(-0.63f - 0.25f - 0.023f, -0.39f - 0.613f);
        private const float FlashTime = 0.1f;              // başarılı parry'de Knight bu kadar süre tam parlak kalır (gerçek saniye)
        private const float FlashFade = 0.06f;             // sonra bu sürede hızla söner
        private const float FlashStrength = 0.8f;          // parlamanın gücü (1 = bembeyaz)
        private const int ShardsHigh = 14;                 // "çok" savrulan saldırılarda saçılan beyaz parça sayısı
        private const int ShardsMax = 24;                  // "max" savrulan saldırılarda  // başarılı parry pozunda kılıcın ucu (yeni ölçek ve konuma göre)
        private const float SoundVolume = 1.0f;            // parry seslerinin seviyesi (artık oyunun ses karıştırıcısından geçiyor; fazla gelirse düşür)
        private const float HurtQuietTime = 0.6f;          // hasar yedikten sonra bu süre parry sesleri çalmaz, hasar sesi öne çıksın
        private const float PrepFrame = 0.04f;             // kılıç çekme karesi başına süre (1-2-3) — 0,3 sn pencereye göre
        private const float PrepLoopFrame = 0.08f;         // 3. ve 4. kare sırayla, pencere sürdükçe
        private const float SuccessFrame1 = 0.06f;         // başarılı parry 1. kare
        private const float SuccessHold = 0.25f;           // başarılı parry pozu toplam süre
        private const float RecoverFrame = 0.08f;          // toparlanma karesi başına süre
        private const float EffectFrame = 0.05f;           // efekt karesi başına süre

        private static readonly Vector2[] PrepPivots = { new Vector2(0.5000f, 0.4264f), new Vector2(0.3531f, 0.4264f), new Vector2(0.4528f, 0.4371f), new Vector2(0.4690f, 0.4469f) };
        private static readonly Vector2[] SuccessPivots = { new Vector2(0.3577f, 0.4187f), new Vector2(0.3125f, 0.4152f) };
        private static readonly Vector2[] RecoverPivots = { new Vector2(0.4897f, 0.4202f), new Vector2(0.4789f, 0.4102f) };
        // Parry olmazsa (sarı pencere boşa biterse) oynayan toparlanma: Parry_FailRecover_1..2.png
        private static readonly Vector2[] FailRecoverPivots = { new Vector2(0.4740f, 0.4352f), new Vector2(0.4789f, 0.4273f) };
        // Duvardaki parry kareleri (Parry_Wall_*.png). Daha küçük çizildiler: kafa boyuna göre aynı boya getirilir.
        // Pivotlar: her karede kafanın ortası aynı yerde kalsın diye (yerdeki karelerle aynı kural).
        private const float WallPPU = 45f;                 // küçük sayı = büyük çizim
        private const float WallXOffset = KnightXOffset;   // duvarda çizimin Knight'a göre kayması (videoya göre ayarlanacak)
        private const float WallYOffset = KnightYOffset;
        private const bool WallMirror = false;             // duvarda ters bakıyorsa true yap
        private const float WallRecoverFrame = 0.1f;
        private static readonly Vector2[] WallPrepPivots = { new Vector2(0.4459f, 0.4312f), new Vector2(0.4578f, 0.4356f), new Vector2(0.4578f, 0.4356f) };
        private static readonly Vector2[] WallFailRecoverPivots = { new Vector2(0.4952f, 0.4165f), new Vector2(0.4836f, 0.4482f) };
        private static readonly Vector2[] WallSuccessPivots = { new Vector2(0.4760f, 0.4517f), new Vector2(0.5243f, 0.3857f) };
        private static readonly Vector2[] WallRecoverPivots = { new Vector2(0.4836f, 0.4482f) };
        private Sprite[] _gPrep, _gSuccess, _gRecover, _gFailRecover, _wPrep, _wSuccess, _wRecover, _wFailRecover;
        private bool _wallMode;
        private static readonly Vector2[] EffectPivots = { new Vector2(0.602f, 0.252f), new Vector2(0.602f, 0.252f), new Vector2(0.754f, 0.335f), new Vector2(0.80f, 0.40f) };

        private static ParryVisuals _inst;
        private ParryMod _mod;
        private Sprite[] _prep, _success, _recover, _failRecover, _effect;
        // Büyük parry'nin kendi kareleri: Parry_Big_1..4 (kayma sırasında), Parry_BigRecover_1..3 (toparlanma).
        // Bu çizimler diğerlerinden küçük ölçekte çizildi: 1,58 kat büyütülerek (PPU / 1,58) aynı boya getirilir.
        private Sprite[] _big, _bigRecover;
        private const float BigScale = 1.58f;
        private static readonly Vector2[] BigPivots = { new Vector2(0.3902f, 0.4550f), new Vector2(0.4716f, 0.4290f), new Vector2(0.4948f, 0.4792f), new Vector2(0.5478f, 0.4843f) };
        private static readonly Vector2[] BigRecoverPivots = { new Vector2(0.4079f, 0.4276f), new Vector2(0.4758f, 0.4290f), new Vector2(0.4836f, 0.4210f) };
        private const float BigRecoverFrame = 0.1f;   // büyük parry toparlanma karesi başına süre
        private bool _bigMode;
        private float _bigSlideTime = 0.6f;
        private Sprite[] _recoverNow;   // o an oynayan toparlanma (başarılı parry sonrası ya da boşa giden parry sonrası)
        private AudioClip _sndPrepare, _sndBig;
        private string _modDir;
        // Başarılı parry sesleri: knight_parry.wav ve knight_parry_*.wav (ör. knight_parry_1.wav, knight_parry_2.wav...).
        // Her parry'de aralarından rastgele biri çalar (aynısı arka arkaya iki kez gelmez).
        private readonly System.Collections.Generic.List<AudioClip> _sndParry = new System.Collections.Generic.List<AudioClip>();
        private int _lastParrySnd = -1;
        private AudioSource _audio;
        private AudioSource _audioPrep;   // hazırlık sesi ayrı kaynakta: başarılı parry olunca tek başına susturulabilsin
        private const float PrepFadeOnSuccess = 0.06f;   // başarılı parry'de hazırlık sesi bu sürede söner

        private enum Phase { None, Prep, Success, Recover, AirFall }

        // ---- Havada parry (Parry_Air_Prep_<sütun>_<1-3>.png, Parry_Air_Fall_1..6.png) ----
        // Basınca: Knight'ın zıplama animasyonunda o an hangi karedeyse o sütunun 3 karesi (süreler yerdeki hazırlık gibi).
        // Parry olmazsa toparlanma yok: yere değene kadar oyunun düşüş kareleri yerine senin 6 düşüş karen, oyunun karesiyle birebir.
        private static readonly Vector2[][] AirPrepPivots =
        {
            new[] { new Vector2(0.4624f, 0.4256f), new Vector2(0.4036f, 0.4176f), new Vector2(0.4744f, 0.4131f), new Vector2(0.4556f, 0.4092f) },
            new[] { new Vector2(0.4624f, 0.4256f), new Vector2(0.3753f, 0.4223f), new Vector2(0.4932f, 0.4129f), new Vector2(0.5080f, 0.4102f) },
            new[] { new Vector2(0.4624f, 0.4256f), new Vector2(0.3796f, 0.4299f), new Vector2(0.5274f, 0.4209f), new Vector2(0.5282f, 0.4174f) },
            new[] { new Vector2(0.4624f, 0.4256f), new Vector2(0.4847f, 0.4435f), new Vector2(0.5698f, 0.4073f), new Vector2(0.5501f, 0.4031f) },
            new[] { new Vector2(0.4624f, 0.4256f), new Vector2(0.4107f, 0.4603f), new Vector2(0.4909f, 0.4183f), new Vector2(0.5166f, 0.4141f) },
        };
        private static readonly Vector2[] AirFallPivots = { new Vector2(0.3902f, 0.4054f), new Vector2(0.3580f, 0.3999f), new Vector2(0.3842f, 0.4102f), new Vector2(0.3839f, 0.4108f), new Vector2(0.3682f, 0.4107f), new Vector2(0.3839f, 0.4108f) };
        private const int AirRiseFrames = 5;          // zıplama animasyonunun ilk 5 karesi = senin 5 sütunun
        private Sprite[][] _airPrep;
        private Sprite[] _airFall;
        private bool _airMode;
        private int _airCol;
        private string _airFallLogKey;

        // Knight'ın o anki zıplama karesinden sütun (0-4).
        private int AirColumn(HeroController h, out string info)
        {
            info = "?";
            try
            {
                tk2dSpriteAnimator a = h.GetComponent<tk2dSpriteAnimator>();
                if (a != null && a.CurrentClip != null)
                {
                    int f = a.CurrentFrame, n = a.CurrentClip.frames != null ? a.CurrentClip.frames.Length : 0;
                    info = "'" + a.CurrentClip.name + "' kare " + (f + 1) + "/" + n;
                    if (a.CurrentClip.name == "Airborne") return Mathf.Clamp(f, 0, AirRiseFrames - 1);
                    if (a.CurrentClip.name.Contains("Fall")) return AirRiseFrames - 1;
                }
            }
            catch (System.Exception) { }
            return AirRiseFrames - 1;
        }

        // Oyunun o anki düşüş karesine karşılık gelen senin düşüş karen (0-5); -1 = düşüş karesi değil.
        private int AirFallIndex(HeroController h)
        {
            try
            {
                tk2dSpriteAnimator a = h.GetComponent<tk2dSpriteAnimator>();
                if (a == null || a.CurrentClip == null) return -1;
                string n = a.CurrentClip.name; int f = a.CurrentFrame, len = a.CurrentClip.frames != null ? a.CurrentClip.frames.Length : 0;
                int idx = -1;
                if (n.Contains("Fall")) idx = f;
                else if (n == "Airborne") idx = f - AirRiseFrames;
                string key = n + "|" + f;
                if (key != _airFallLogKey)
                {
                    _airFallLogKey = key;
                }
                return idx < 0 ? -1 : Mathf.Clamp(idx, 0, (_airFall != null ? _airFall.Length : 6) - 1);
            }
            catch (System.Exception) { return -1; }
        }
        private Phase _phase = Phase.None;
        private float _phaseStart;
        private HeroController _hero;
        private MeshRenderer _heroR;
        private GameObject _overlay;
        private SpriteRenderer _sr;
        private Sprite _show;
        private int _lastHealth = -1;
        private SpriteRenderer _flashSr;
        private float _flashStart = -10f;
        private int _flashFrames;          // teşhis: parlamanın kaç karede ekrana çizildiği
        private bool _flashLogPending;
        private static Sprite _shardSprite;
        private float _hurtUntil;

        internal static ParryVisuals Instance(ParryMod mod)
        {
            if (_inst == null)
            {
                GameObject go = new GameObject("ParryVisuals (ParryMod)");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<ParryVisuals>();
                _inst._mod = mod;
                _inst.Load();
            }
            return _inst;
        }

        private void Load()
        {
            string dir = ParryAssets.Dir("Parry");
            string sdir = ParryAssets.Dir("Sounds");
            _prep = LoadSet(dir, "Parry_Prep_", 4, PrepPivots, KnightPPU);
            _success = LoadSet(dir, "Parry_Success_", 2, SuccessPivots, KnightPPU);
            _recover = LoadSet(dir, "Parry_Recover_", 2, RecoverPivots, KnightPPU);
            _big = LoadSet(dir, "Parry_Big_", 4, BigPivots, KnightPPU / BigScale);
            _bigRecover = LoadSet(dir, "Parry_BigRecover_", 3, BigRecoverPivots, KnightPPU / BigScale);
            _failRecover = LoadSet(dir, "Parry_FailRecover_", 2, FailRecoverPivots, KnightPPU);
            _effect = LoadSet(dir, "Parry_Effect_", 4, EffectPivots, EffectPPU);
            _gPrep = _prep; _gSuccess = _success; _gRecover = _recover; _gFailRecover = _failRecover;
            LoadAltVariants(dir);
            _airPrep = new Sprite[5][];
            int airCols = 0;
            for (int c = 0; c < 5; c++) { _airPrep[c] = LoadSet(dir, "Parry_Air_Prep_" + (c + 1) + "_", 4, AirPrepPivots[c], KnightPPU); if (_airPrep[c] != null) airCols++; }
            if (airCols == 0) _airPrep = null;
            _airFall = LoadSet(dir, "Parry_Air_Fall_", 6, AirFallPivots, KnightPPU);
            _wPrep = LoadSet(dir, "Parry_Wall_Prep_", 3, WallPrepPivots, WallPPU);
            _wSuccess = LoadSet(dir, "Parry_Wall_Success_", 2, WallSuccessPivots, WallPPU);
            _wRecover = LoadSet(dir, "Parry_Wall_Recover_", 1, WallRecoverPivots, WallPPU);
            _wFailRecover = LoadSet(dir, "Parry_Wall_FailRecover_", 2, WallFailRecoverPivots, WallPPU);
            _sndPrepare = LoadWav(System.IO.Path.Combine(sdir, "knight_parry_prepare.wav"));
            _sndBig = LoadWav(System.IO.Path.Combine(sdir, "big_parry.wav"));   // max savrulan (büyük) parry sesi
            _modDir = ParryAssets.ModDir;
            var parryFiles = new System.Collections.Generic.List<string>();
            string mainParry = System.IO.Path.Combine(sdir, "knight_parry.wav");
            if (System.IO.File.Exists(mainParry)) parryFiles.Add(mainParry);
            if (System.IO.Directory.Exists(sdir))
            foreach (string f in System.IO.Directory.GetFiles(sdir, "knight_parry_*.wav"))
                if (!System.IO.Path.GetFileName(f).ToLowerInvariant().StartsWith("knight_parry_prepare")) parryFiles.Add(f);
            foreach (string f in parryFiles)
            {
                AudioClip c = LoadWav(f);
                if (c != null) _sndParry.Add(c);
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; _audio.spatialBlend = 0f;
            _audioPrep = gameObject.AddComponent<AudioSource>();
            _audioPrep.playOnAwake = false; _audioPrep.spatialBlend = 0f;
            Camera.onPreCull += Apply;
            Camera.onPostRender += Restore;
        }

        private static int Count(Sprite[] a) { return a != null ? a.Length : 0; }

        // ---- Başarılı parry'nin alternatif animasyonları (rastgele) ----
        // Assets\Parry\Parry_Success_V<n>_1.png, _2 ... ve Parry_Recover_V<n>_1.png ... (n = 2, 3, 4 ...).
        // Yeni bir alternatif eklemek için sadece dosyaları koyman yeterli: boyut ve hizalama kafadan otomatik hesaplanır.
        private const float RefHeadHeight = 76f;   // yerdeki çizimlerde kafanın (boynuzlarla) piksel boyu
        private const float RefFeetBelow = 53f;    // yerdeki çizimlerde ayak tabanı pivotun bu kadar altında
        private readonly System.Collections.Generic.List<Sprite[]> _altSuccess = new System.Collections.Generic.List<Sprite[]>();
        private readonly System.Collections.Generic.List<Sprite[]> _altRecover = new System.Collections.Generic.List<Sprite[]>();
        private int _lastVariant = -1;
        private const float RepeatVariantChance = 0.25f;

        private void LoadAltVariants(string dir)
        {
            _altSuccess.Clear(); _altRecover.Clear();
            for (int v = 2; v <= 9; v++)
            {
                Sprite[] suc = LoadAuto(dir, "Parry_Success_V" + v + "_");
                if (suc == null) continue;
                _altSuccess.Add(suc);
                _altRecover.Add(LoadAuto(dir, "Parry_Recover_V" + v + "_"));
            }
        }

        // Dosyaları yükler; ölçeği ve pivotu her karedeki kafadan (beyaz maske) hesaplar.
        private Sprite[] LoadAuto(string dir, string prefix)
        {
            var texs = new System.Collections.Generic.List<Texture2D>();
            for (int i = 1; i <= 12; i++)
            {
                string path = System.IO.Path.Combine(dir, prefix + i + ".png");
                if (!System.IO.File.Exists(path)) break;
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                texs.Add(t);
            }
            if (texs.Count == 0) return null;
            var heads = new Vector3[texs.Count];   // x = merkez x, y = merkez y (alttan), z = kafa boyu
            float sumH = 0f; int nH = 0;
            for (int i = 0; i < texs.Count; i++) { heads[i] = FindHead(texs[i]); if (heads[i].z > 0f) { sumH += heads[i].z; nH++; } }
            float avgH = nH > 0 ? sumH / nH : RefHeadHeight;
            float k = avgH / RefHeadHeight;
            float ppu = KnightPPU * k;
            float below = RefFeetBelow * k;
            var list = new Sprite[texs.Count];
            for (int i = 0; i < texs.Count; i++)
            {
                Texture2D t = texs[i];
                // Yatayda kafanın ortası, dikeyde ayak tabanı hizalanır (boy farklı çizilse de ayaklar yere basar).
                float feet = FeetRow(t);
                float px = heads[i].z > 0f ? heads[i].x / t.width : 0.5f;
                Vector2 piv = new Vector2(px, Mathf.Clamp01((feet + below) / t.height));
                list[i] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), piv, ppu);
            }
            return list;
        }

        // Çizimin en alttaki dolu satırı (alttan sayılır): ayak tabanı.
        private static float FeetRow(Texture2D t)
        {
            Color32[] px = t.GetPixels32();
            int w = t.width, h = t.height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 128) return y;
            return 0f;
        }

        // Karedeki en büyük beyaz bölge (maske + boynuzlar): merkezi ve boyu.
        private static Vector3 FindHead(Texture2D t)
        {
            int w = t.width, h = t.height;
            Color32[] px = t.GetPixels32();
            bool[] white = new bool[w * h];
            for (int i = 0; i < px.Length; i++) { Color32 c = px[i]; white[i] = c.a > 200 && c.r > 215 && c.g > 215 && c.b > 215; }
            int[] label = new int[w * h];
            int best = 0, bestId = 0, id = 0;
            var q = new System.Collections.Generic.Queue<int>();
            float bx = 0, by = 0; int bminY = 0, bmaxY = 0;
            for (int s0 = 0; s0 < white.Length; s0++)
            {
                if (!white[s0] || label[s0] != 0) continue;
                id++; int cnt = 0; long sx = 0, sy = 0; int minY = int.MaxValue, maxY = int.MinValue;
                label[s0] = id; q.Enqueue(s0);
                while (q.Count > 0)
                {
                    int p = q.Dequeue(); int x = p % w, y = p / w;
                    cnt++; sx += x; sy += y; if (y < minY) minY = y; if (y > maxY) maxY = y;
                    if (x > 0 && white[p - 1] && label[p - 1] == 0) { label[p - 1] = id; q.Enqueue(p - 1); }
                    if (x < w - 1 && white[p + 1] && label[p + 1] == 0) { label[p + 1] = id; q.Enqueue(p + 1); }
                    if (y > 0 && white[p - w] && label[p - w] == 0) { label[p - w] = id; q.Enqueue(p - w); }
                    if (y < h - 1 && white[p + w] && label[p + w] == 0) { label[p + w] = id; q.Enqueue(p + w); }
                }
                if (cnt > best) { best = cnt; bestId = id; bx = sx / (float)cnt; by = sy / (float)cnt; bminY = minY; bmaxY = maxY; }
            }
            if (best == 0) return Vector3.zero;
            return new Vector3(bx, by, bmaxY - bminY + 1);   // GetPixels32 alttan başlar: y zaten alttan
        }

        private Sprite[] LoadSet(string dir, string prefix, int n, Vector2[] pivots, float ppu)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 1; i <= n; i++)
            {
                string path = System.IO.Path.Combine(dir, prefix + i + ".png");
                if (!System.IO.File.Exists(path)) { if (i == 1) return null; break; }
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                list.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivots[Mathf.Min(i - 1, pivots.Length - 1)], ppu));
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        // 16 bit PCM .wav okuyucu (ek kütüphane gerekmez).
        internal AudioClip LoadWavPublic(string path) { return LoadWav(path); }
        private AudioClip LoadWav(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                byte[] b = System.IO.File.ReadAllBytes(path);
                int ch = 2, rate = 44100, bits = 16, pos = 12, dataPos = -1, dataLen = 0;
                while (pos + 8 <= b.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                    int len = System.BitConverter.ToInt32(b, pos + 4);
                    if (id == "fmt ") { ch = System.BitConverter.ToInt16(b, pos + 10); rate = System.BitConverter.ToInt32(b, pos + 12); bits = System.BitConverter.ToInt16(b, pos + 22); }
                    else if (id == "data") { dataPos = pos + 8; dataLen = Mathf.Min(len, b.Length - dataPos); break; }
                    pos += 8 + len + (len & 1);
                }
                if (dataPos < 0 || bits != 16) {  return null; }
                int samples = dataLen / 2;
                float[] data = new float[samples];
                for (int i = 0; i < samples; i++) data[i] = System.BitConverter.ToInt16(b, dataPos + i * 2) / 32768f;
                AudioClip clip = AudioClip.Create(System.IO.Path.GetFileNameWithoutExtension(path), samples / ch, ch, rate, false);
                clip.SetData(data, 0);
                return clip;
            }
            catch (System.Exception) {  return null; }
        }

        private void PlaySound(AudioClip c) { PlaySound(c, _audio, 1f); }
        private void PlaySound(AudioClip c, AudioSource src) { PlaySound(c, src, 1f); }

        private void PlaySound(AudioClip c, AudioSource src, float mul)
        {
            if (mul <= 0f) return;
            if (c == null || src == null || _audio == null) return;
            if (Time.time < _hurtUntil) return;   // hasar sesi öne çıksın
            // Knight'ın kendi seslerinin (vurma sesi dahil) geçtiği ses karıştırıcısına bağlan:
            // oyunun ses ayarları ve kısma oranı aynen uygulanır.
            if (_audio.outputAudioMixerGroup == null && HeroController.instance != null)
            {
                AudioSource heroSrc = HeroController.instance.GetComponent<AudioSource>();
                if (heroSrc != null && heroSrc.outputAudioMixerGroup != null)
                {
                    _audio.outputAudioMixerGroup = heroSrc.outputAudioMixerGroup;
                    if (_audioPrep != null) _audioPrep.outputAudioMixerGroup = heroSrc.outputAudioMixerGroup;
                }
            }
            float vol = SoundVolume;
            if (_audio.outputAudioMixerGroup == null)   // karıştırıcı bulunamazsa eski yol: ayarları elle uygula
            {
                try
                {
                    var gs = GameManager.instance.gameSettings;
                    vol *= Mathf.Clamp01(gs.masterVolume / 10f) * Mathf.Clamp01(gs.soundVolume / 10f) * 0.5f;
                }
                catch { }
            }
            vol *= mul;
            if (src == _audioPrep) { src.Stop(); src.clip = c; src.volume = vol; src.Play(); }   // Play: isPlaying güvenilir, tek başına söndürülebilir
            else src.PlayOneShot(c, vol);
        }

        private int _prepFadeToken;
        private System.Collections.IEnumerator FadePrep(int token)
        {
            float t = 0f, v0 = _audioPrep.volume;
            while (t < PrepFadeOnSuccess && token == _prepFadeToken)
            {
                t += Time.unscaledDeltaTime;
                _audioPrep.volume = v0 * (1f - t / PrepFadeOnSuccess);
                yield return null;
            }
            if (token == _prepFadeToken) _audioPrep.Stop();
        }

        // ---- Mod'dan çağrılanlar ----
        // Hazırlık sesi: başarılı parry sesi çaldıktan sonra PrepareSoundGap saniye boyunca hazırlık sesi çalmaz
        // (art arda parry'lerde kulağa batmasın). Parry olmadıysa her basışta çalar.
        private const float PrepareSoundGap = 1.0f;
        private float _lastSuccessTime = -10f;

        internal void NotePress() { }   // eski sürümden kalma çağrı; artık bir şey yapmıyor

        internal void OnPress(HeroController h)
        {
            if (Time.time - _lastSuccessTime >= PrepareSoundGap) { _prepFadeToken++; PlaySound(_sndPrepare, _audioPrep); }
            // Başarılı parry animasyonu (ve onun toparlanması) sürerken basıldıysa hazırlık animasyonu girmez:
            // mevcut animasyon devam eder, pencere arkada açık. Bu parry de başarılı olursa doğrudan yeni başarılı animasyona geçilir.
            if (_fromSuccess && !_bigMode && (_phase == Phase.Success || _phase == Phase.Recover))
            {
                return;
            }
            Begin(h, Phase.Prep);
        }

        private bool _fromSuccess;
        private bool _attackAtStart;   // şu an oynayan Success/Recover, başarılı bir parry'den mi geliyor

        internal void OnSuccess(HeroController h)
        {
            // Başarılı parry sesi hazırlık sesinin altında kalmasın: hazırlık sesi hızla susturulur.
            if (_audioPrep != null && _audioPrep.isPlaying) StartCoroutine(FadePrep(++_prepFadeToken));
            if (_sndParry.Count > 0)
            {
                // Animasyondaki gibi: bir önceki sesin tekrar gelme şansı %25; değilse diğerlerinden biri eşit şansla.
                int n = _sndParry.Count, k;
                if (_lastParrySnd < 0 || _lastParrySnd >= n || n == 1) k = UnityEngine.Random.Range(0, n);
                else if (UnityEngine.Random.value < RepeatVariantChance) k = _lastParrySnd;
                else { k = UnityEngine.Random.Range(0, n - 1); if (k >= _lastParrySnd) k++; }
                _lastParrySnd = k;
                PlaySound(_sndParry[k], _audio, ParryMod.ParryVolumeMul);
                _lastSuccessTime = Time.time;
            }
            Begin(h, Phase.Success);
            _flashStart = Time.unscaledTime;
            _flashFrames = 0; _flashLogPending = true;
            SpawnEffect(h);
        }

        private void Begin(HeroController h, Phase p)
        {
            _hero = h;
            if (_heroR == null || _heroR.gameObject != h.gameObject) _heroR = h.GetComponent<MeshRenderer>();
            if (_overlay == null)
            {
                _overlay = new GameObject("Parry Anim (ParryMod)");
                _sr = _overlay.AddComponent<SpriteRenderer>();
                // Parlama: aynı kare, düz beyaz boyanmış halde üstte (GUI/Text Shader resmin sadece şeklini kullanır).
                GameObject fl = new GameObject("Parry Flash (ParryMod)");
                fl.transform.SetParent(_overlay.transform, false);
                fl.transform.localPosition = new Vector3(0f, 0f, -0.0005f);
                _flashSr = fl.AddComponent<SpriteRenderer>();
                Shader sh = Shader.Find("GUI/Text Shader");
                if (sh != null) _flashSr.material = new Material(sh);

                _flashSr.enabled = false;
            }
            _overlay.transform.SetParent(h.transform, false);
            // Duvarda mı? Duvar kareleri varsa onlar, yoksa yerdekiler (duvarda yerdeki kareler oynamaz).
            _wallMode = ParryMod.OnWall(h) && _wPrep != null;
            _airMode = p == Phase.Prep && !_wallMode && !h.cState.onGround && !ParryMod.OnWall(h) && _airPrep != null;
            if (_airMode)
            {
                string info;
                _airCol = AirColumn(h, out info);
                if (_airPrep[_airCol] == null) _airMode = false;
                _airFallLogKey = null;
            }
            _prep = _wallMode ? _wPrep : _gPrep;
            _success = _wallMode ? (_wSuccess ?? _gSuccess) : _gSuccess;
            _recover = _wallMode ? (_wRecover ?? _gRecover) : _gRecover;
            _failRecover = _wallMode ? (_wFailRecover ?? _wRecover) : _gFailRecover;
            // Yerde başarılı parry: asıl animasyon ve alternatifler arasından rastgele biri.
            if (p == Phase.Success && !_wallMode && _altSuccess.Count > 0)
            {
                // Bir önceki animasyonun tekrar gelme şansı %25; değilse diğerlerinden biri eşit şansla.
                int total = _altSuccess.Count + 1;
                int v;
                if (_lastVariant < 0 || UnityEngine.Random.value < RepeatVariantChance) v = _lastVariant < 0 ? UnityEngine.Random.Range(0, total) : _lastVariant;
                else { v = UnityEngine.Random.Range(0, total - 1); if (v >= _lastVariant) v++; }
                _lastVariant = v;
                if (v > 0) { _success = _altSuccess[v - 1]; _recover = _altRecover[v - 1] ?? _gRecover; }
            }
            _overlay.transform.localPosition = _wallMode ? new Vector3(WallXOffset, WallYOffset, -0.001f) : new Vector3(KnightXOffset, KnightYOffset, -0.001f);
            _overlay.transform.localScale = _wallMode && WallMirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;   // Knight'ın kendi çizimleri gibi: Knight hangi yöne bakıyorsa o yöne
            if (_heroR != null)
            {
                _sr.sortingLayerID = _heroR.sortingLayerID; _sr.sortingOrder = _heroR.sortingOrder;
                if (_flashSr != null) { _flashSr.sortingLayerID = _heroR.sortingLayerID; _flashSr.sortingOrder = _heroR.sortingOrder + 1; }
            }
            _phase = p; _phaseStart = Time.time; _bigMode = false;
            _attackAtStart = h.cState.attacking;   // vurduktan hemen sonra basıldıysa o vuruş animasyonu kesmesin (sadece yeni vuruş keser)
            if (p == Phase.Success) _fromSuccess = true; else if (p == Phase.Prep) _fromSuccess = false;
        }

        private void Stop()
        {
            _phase = Phase.None; _show = null;
            if (_sr != null) _sr.enabled = false;
            if (_flashSr != null) _flashSr.enabled = false;
            // Knight'ın kendi çizimine dokunmuyoruz: onu sadece çizim anında gizleyip hemen geri açıyoruz (Apply/Restore).
            // Eskiden burada açılıyordu; ölürken oyunun gizlediği Knight'ı geri açıp ölüm animasyonunu bozuyordu.
        }

        private void Update()
        {
            // Hasar yenince çalan parry sesini hemen kes: hasar sesi öne çıksın.
            PlayerData pd = PlayerData.instance;
            if (pd != null)
            {
                int hp = pd.health + pd.healthBlue;
                if (_lastHealth >= 0 && hp < _lastHealth)
                {
                    if (_audio != null) _audio.Stop();
                    if (_audioPrep != null) _audioPrep.Stop();
                    _hurtUntil = Time.time + HurtQuietTime;
                }
                _lastHealth = hp;
            }
            if (_flashLogPending && Time.unscaledTime - _flashStart > FlashTime + FlashFade + 0.05f)
            {
                _flashLogPending = false;
            }
            if (_phase == Phase.None) return;
            HeroController h = _hero;
            if (h != null && !h.cState.attacking) _attackAtStart = false;
            if (h == null || h.cState.dead || (h.cState.attacking && !_attackAtStart) || h.cState.dashing || h.cState.casting || h.cState.recoiling
                || (h.cState.jumping && !_airMode) || h.cState.doubleJumping || h.cState.bouncing || h.cState.transitioning
                || (_airMode && _phase == Phase.AirFall && h.cState.onGround)   // havada parry düşüşü: yere değince biter
                || (ParryMod.OnWall(h) != _wallMode))   // duvardayken duvar kareleri; duvardan ayrılınca (ya da duvara değince) biter
            { Stop(); return; }
            float t = Time.time - _phaseStart;
            switch (_phase)
            {
                case Phase.AirFall:
                    {
                        if (_airFall == null) { Stop(); return; }
                        int fi = AirFallIndex(h);
                        if (fi >= 0) _show = _airFall[fi];
                        else _show = null;   // henüz yükselişteyse oyunun kendi karesi görünür
                        break;
                    }
                case Phase.Prep:
                    if (_airMode)
                    {
                        if (Time.time > _mod.WindowEnd && t > 0.05f)   // parry olmadı: toparlanma yok, düşüş kareleri
                        { if (_airFall != null) Next(Phase.AirFall); else Stop(); return; }
                        Sprite[] col = _airPrep[_airCol];
                        // 1. ve 2. kare yerdeki gibi kısa; kalan pencere 3. ve 4. kare arasında yarı yarıya (4. kare: pelerin/kafa hafif oynar).
                        float restA = Mathf.Max(0.04f, _mod.WindowEnd - _phaseStart - 2f * PrepFrame);
                        int af;
                        if (t < PrepFrame) af = 0;
                        else if (t < 2f * PrepFrame) af = 1;
                        else if (t < 2f * PrepFrame + restA * 0.5f) af = 2;
                        else af = 3;
                        _show = col[Mathf.Min(af, col.Length - 1)];
                        break;
                    }
                    if (_prep == null) { _show = null; break; }
                    if (Time.time > _mod.WindowEnd && t > 0.05f)   // sarı halka bitti, parry olmadı
                    { _recoverNow = _failRecover ?? _recover; Next(Phase.Recover); return; }
                    if (_wallMode)
                    {
                        // Duvar: 1. kare kısa, kalan pencere 2. ve 3. kare arasında yarı yarıya.
                        float rem = Mathf.Max(0.06f, _mod.WindowEnd - _phaseStart - PrepFrame);
                        if (t < PrepFrame || _prep.Length < 2) _show = _prep[0];
                        else if (t < PrepFrame + rem * 0.5f || _prep.Length < 3) _show = _prep[1];
                        else _show = _prep[2];
                        break;
                    }
                    int intro = Mathf.Min(3, _prep.Length);           // 1-2-3 bir kez
                    int f = (int)(t / PrepFrame);
                    if (f < intro) _show = _prep[f];
                    else if (_prep.Length < 4) _show = _prep[_prep.Length - 1];
                    else _show = _prep[2 + ((int)((t - intro * PrepFrame) / PrepLoopFrame) & 1)];   // 4-3-4-3...
                    break;
                case Phase.Success:
                    if (_bigMode && _big != null)
                    {
                        // Büyük parry: kayma boyunca 4 kare sırayla (yavaş çekim gibi), sonra son karede bekler.
                        if (Time.time >= _successHoldUntil) { _recoverNow = _bigRecover ?? _recover; Next(Phase.Recover); return; }
                        float bf = Mathf.Max(0.05f, _bigSlideTime / _big.Length);
                        _show = _big[Mathf.Min((int)(t / bf), _big.Length - 1)];
                        break;
                    }
                    if (_success == null || (t >= SuccessHold && Time.time >= _successHoldUntil)) { _recoverNow = _recover; Next(Phase.Recover); return; }
                    if (t < SuccessFrame1 || _success.Length < 2) _show = _success[0];
                    else
                    {
                        // 1. kare kısa; kalan kareler (2, 3, ...) poz süresine eşit bölünür, sonuncusu süre bitene kadar kalır.
                        int rest = _success.Length - 1;
                        float seg = Mathf.Max(0.02f, (SuccessHold - SuccessFrame1) / rest);
                        _show = _success[Mathf.Min(_success.Length - 1, 1 + (int)((t - SuccessFrame1) / seg))];
                    }
                    break;
                case Phase.Recover:
                    if (_recoverNow == null) { Stop(); return; }
                    int i = (int)(t / (_recoverNow == _bigRecover ? BigRecoverFrame : _wallMode ? WallRecoverFrame : RecoverFrame));
                    if (i >= _recoverNow.Length) { Stop(); return; }
                    _show = _recoverNow[i];
                    break;
            }
        }

        private void Next(Phase p) { _phase = p; _phaseStart = Time.time; Update(); }

        // Ekran çizilmeden hemen önce: bizim kare varsa Knight'ın kendi çizimi yerine o görünür.
        private void Apply(Camera cam)
        {
            if (_sr == null) return;
            // Oyun Knight'ı kendisi gizlemişse (ölüm, sahne geçişi vb.) bizim kare de görünmesin.
            bool gameShowsHero = _heroR == null || _heroR.enabled;
            if (_phase == Phase.None || _show == null || !gameShowsHero)
            {
                _sr.enabled = false;
                if (_flashSr != null) _flashSr.enabled = false;
                return;
            }
            _sr.sprite = _show; _sr.enabled = true;
            if (_heroR != null) { _heroR.enabled = false; _hidHero = true; }
            // Beyaz parlama: hemen tam güçte başlar, FlashTime içinde söner.
            if (_flashSr != null && _flashSr.material != null)
            {
                float el = Time.unscaledTime - _flashStart;
                if (el >= 0f && el < FlashTime + FlashFade)
                {
                    float a = el < FlashTime ? 1f : 1f - (el - FlashTime) / FlashFade;   // önce tam parlak, sonra hızla söner
                    _flashSr.sprite = _show;
                    _flashSr.color = new Color(1f, 1f, 1f, FlashStrength * a);
                    _flashSr.enabled = true;
                    _flashFrames++;
                }
                else _flashSr.enabled = false;
            }
        }

        // Kamera çizimi bitince Knight'ın kendi çizimini hemen eski haline getir (kalıcı olarak kapatmayız).
        private bool _hidHero;
        private void Restore(Camera cam)
        {
            if (_hidHero && _heroR != null) _heroR.enabled = true;
            _hidHero = false;
        }

        private static Vector3 EffectPoint(HeroController h)
        {
            float face = h.transform.localScale.x >= 0f ? 1f : -1f;   // Knight'ın çizim yönü (çizimler sola bakar)
            return h.transform.position + new Vector3(NailTip.x * face, NailTip.y, -0.01f);
        }

        // ===== Büyük parry ("max" savrulan saldırılar, Sekiro'daki gibi) =====
        private const int BigShards = 42;            // saçılan uzun beyaz çizgiler
        private const int BigSparks = 18;            // yavaşça süzülen küçük beyaz kıvılcımlar
        private float _successHoldUntil;
        private static Sprite _glowSprite;

        // Büyük parry kayması bitti: duruş pozunu bırak; toparlanma karelerinin süresini döndür (kilit için).
        internal float EndBigHold()
        {
            _successHoldUntil = Time.time;
            if (_bigMode && _big != null && _bigRecover != null) return _bigRecover.Length * BigRecoverFrame + 0.02f;
            int n = _recover != null ? _recover.Length : 0;
            float left = 0f;
            if (_phase == Phase.Success) left = Mathf.Max(0f, SuccessHold - (Time.time - _phaseStart));
            return left + n * RecoverFrame + 0.02f;
        }

        // Tılsım alınınca kısa beyaz parlama (büyük parry efektinin sessiz hali).
        internal void BigParry(HeroController h, float slideTime)
        {
            if (h == null) return;
            // Ses: normal parry sesini kes, büyük parry sesini çal.
            if (_sndBig != null) { if (_audio != null) _audio.Stop(); PlaySound(_sndBig, _audio, ParryMod.BigParryVolumeMul); }

            _lastSuccessTime = Time.time;
            // Oyun yavaşlamaz; Knight kayarken başarılı parry pozunda kalır (yeni durma animasyonu gelince o oynar).
            _successHoldUntil = Time.time + slideTime * 1.3f + 1f;   // kayma bitince EndBigHold ile bırakılır
            // Büyük parry'nin kendi kareleri (varsa) kayma başından itibaren oynar.
            if (_phase == Phase.Success && _big != null) { _bigMode = true; _bigSlideTime = slideTime; _phaseStart = Time.time; }
            BigBurst(h);
        }

        private void EnsureShardSprite()
        {
            if (_shardSprite != null) return;
            // İnce, uçları yumuşak beyaz çizgi (32x4 px).
            Texture2D t = new Texture2D(32, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 4; y++)
                {
                    float ex = Mathf.Min(x, 31 - x) / 6f, ey = (y == 0 || y == 3) ? 0.45f : 1f;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ex) * ey));
                }
            t.Apply();
            _shardSprite = Sprite.Create(t, new Rect(0, 0, 32, 4), new Vector2(0.5f, 0.5f), 64f);
        }

        internal static Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, d * d));
                }
            t.Apply();
            _glowSprite = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 birim çap
            return _glowSprite;
        }

        // Bol ama düzenli beyaz efekt: büyük kılıç efekti, yumuşak beyaz parlama, öne doğru yoğun çizgiler ve süzülen kıvılcımlar.
        private void BigBurst(HeroController h)
        {
            EnsureShardSprite();
            Vector3 p = EffectPoint(h);
            float face = h.transform.localScale.x >= 0f ? 1f : -1f;
            float fwd = face > 0f ? 180f : 0f;   // çizimler sola bakar: localScale +1 iken ileri = sol
            int order = _heroR != null ? _heroR.sortingOrder : 0;
            int layer = _heroR != null ? _heroR.sortingLayerID : 0;

            // Büyük kılıç efekti (iki kat, biri hafif dönük)
            SpawnEffectScaled(h, 1.6f, 0f);
            SpawnEffectScaled(h, 1.25f, 18f);

            // Yumuşak beyaz parlama
            GameObject g = new GameObject("Big Parry Glow (ParryMod)");
            g.transform.position = p + new Vector3(0f, 0f, -0.02f);
            SpriteRenderer gr = g.AddComponent<SpriteRenderer>();
            gr.sprite = GlowSprite(); gr.sortingLayerID = layer; gr.sortingOrder = order + 3;
            StartCoroutine(GlowRoutine(g, gr, 0.4f, 3.2f, 0.45f));

            // Öne doğru yoğun, her yöne seyrek uzun çizgiler
            for (int i = 0; i < BigShards; i++)
            {
                bool cone = i < BigShards * 0.7f;
                float ang = cone ? fwd + UnityEngine.Random.Range(-65f, 65f) : UnityEngine.Random.Range(0f, 360f);
                float speed = UnityEngine.Random.Range(9f, 20f) * (cone ? 1f : 0.7f);
                float len = UnityEngine.Random.Range(0.7f, 1.9f) * (cone ? 1f : 0.7f);
                float life = UnityEngine.Random.Range(0.3f, 0.6f);
                MakeShard(p, layer, order + 2, ang, speed, len, life, UnityEngine.Random.Range(0.8f, 1.3f), 0f);
            }
            // Küçük kıvılcımlar: yavaş, hafifçe aşağı süzülür
            for (int i = 0; i < BigSparks; i++)
            {
                float ang = fwd + UnityEngine.Random.Range(-90f, 90f);
                MakeShard(p, layer, order + 2, ang, UnityEngine.Random.Range(3f, 7f), 0.12f, UnityEngine.Random.Range(0.6f, 1.0f), 1.6f, 3f);
            }
        }

        private void MakeShard(Vector3 p, int layer, int order, float ang, float speed, float len, float life, float thick, float gravity)
        {
            GameObject s = new GameObject("Big Parry Shard (ParryMod)");
            s.transform.position = p;
            SpriteRenderer r = s.AddComponent<SpriteRenderer>();
            r.sprite = _shardSprite; r.sortingLayerID = layer; r.sortingOrder = order;
            StartCoroutine(ShardRoutine(s, r, ang, speed, len, life, true, thick, gravity));
        }

        private System.Collections.IEnumerator GlowRoutine(GameObject g, SpriteRenderer r, float from, float to, float life)
        {
            float t = 0f;
            while (t < life && g != null)
            {
                float k = t / life;
                float sc = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
                g.transform.localScale = new Vector3(sc, sc, 1f);
                r.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
                t += Time.deltaTime;
                yield return null;
            }
            if (g != null) Destroy(g);
        }

        private void SpawnEffectScaled(HeroController h, float scale, float rot)
        {
            if (_effect == null) return;
            float face = h.transform.localScale.x >= 0f ? 1f : -1f;
            GameObject fx = new GameObject("Big Parry Effect (ParryMod)");
            fx.transform.position = EffectPoint(h);
            fx.transform.localScale = new Vector3(face * (EffectMirror ? -1f : 1f) * scale, scale, 1f);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, rot * face);
            SpriteRenderer r = fx.AddComponent<SpriteRenderer>();
            if (_heroR != null) { r.sortingLayerID = _heroR.sortingLayerID; r.sortingOrder = _heroR.sortingOrder + 1; }
            StartCoroutine(PlayEffect(fx, r));
        }

        // "Çok" ve "max" savrulan saldırılar: kılıcın önünden etrafa saçılan düz beyaz parçalar.
        internal void Burst(HeroController h, bool max)
        {
            if (h == null) return;
            if (_shardSprite == null)
            {
                // İnce, uçları yumuşak beyaz çizgi (32x4 px).
                Texture2D t = new Texture2D(32, 4, TextureFormat.RGBA32, false);
                for (int x = 0; x < 32; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        float ex = Mathf.Min(x, 31 - x) / 6f, ey = (y == 0 || y == 3) ? 0.45f : 1f;
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ex) * ey));
                    }
                t.Apply();
                _shardSprite = Sprite.Create(t, new Rect(0, 0, 32, 4), new Vector2(0.5f, 0.5f), 64f);
            }
            Vector3 p = EffectPoint(h);
            int n = max ? ShardsMax : ShardsHigh;
            for (int i = 0; i < n; i++)
            {
                GameObject s = new GameObject("Parry Shard (ParryMod)");
                s.transform.position = p;
                SpriteRenderer r = s.AddComponent<SpriteRenderer>();
                r.sprite = _shardSprite;
                if (_heroR != null) { r.sortingLayerID = _heroR.sortingLayerID; r.sortingOrder = _heroR.sortingOrder + 2; }
                float ang = UnityEngine.Random.Range(0f, 360f);
                float speed = UnityEngine.Random.Range(7f, 14f) * (max ? 1.2f : 1f);
                float len = UnityEngine.Random.Range(0.6f, 1.2f) * (max ? 1.2f : 1f);
                float life = UnityEngine.Random.Range(0.22f, 0.38f);
                StartCoroutine(ShardRoutine(s, r, ang, speed, len, life));
            }
        }

        private System.Collections.IEnumerator ShardRoutine(GameObject s, SpriteRenderer r, float ang, float speed, float len, float life)
        {
            return ShardRoutine(s, r, ang, speed, len, life, false, 1f, 0f);
        }

        // scaled = true: oyun zamanıyla hareket eder (büyük parry'nin ağır çekiminde parçalar da yavaşlar).
        private System.Collections.IEnumerator ShardRoutine(GameObject s, SpriteRenderer r, float ang, float speed, float len, float life,
            bool scaled, float thick, float gravity)
        {
            Vector3 dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
            s.transform.rotation = Quaternion.Euler(0f, 0f, ang);
            float t = 0f;
            while (t < life && s != null)
            {
                float k = t / life;
                float dt = scaled ? Time.deltaTime : Time.unscaledDeltaTime;   // normal parry: yavaşlamada da hemen saçılsın
                s.transform.position += (dir * speed * (1f - k) + Vector3.down * gravity * t) * dt;
                s.transform.localScale = new Vector3(len * (1f - 0.6f * k), thick * (1f - 0.5f * k), 1f);
                r.color = new Color(1f, 1f, 1f, 1f - k * k);
                t += dt;
                yield return null;
            }
            if (s != null) Destroy(s);
        }

        private void SpawnEffect(HeroController h)
        {
            if (_effect == null) return;
            float face = h.transform.localScale.x >= 0f ? 1f : -1f;   // Knight'ın çizim yönü
            Vector3 p = EffectPoint(h);
            GameObject fx = new GameObject("Parry Effect (ParryMod)");
            fx.transform.position = p;
            fx.transform.localScale = new Vector3(face * (EffectMirror ? -1f : 1f), 1f, 1f);
            SpriteRenderer r = fx.AddComponent<SpriteRenderer>();
            if (_heroR != null) { r.sortingLayerID = _heroR.sortingLayerID; r.sortingOrder = _heroR.sortingOrder + 1; }
            StartCoroutine(PlayEffect(fx, r));
        }

        private System.Collections.IEnumerator PlayEffect(GameObject fx, SpriteRenderer r)
        {
            for (int i = 0; i < _effect.Length; i++)
            {
                if (fx == null) yield break;
                r.sprite = _effect[i];
                yield return new WaitForSeconds(EffectFrame);
            }
            if (fx != null) Destroy(fx);
        }
    }


    // =====================================================================
    //  Dosya düzeni (Mods\ParryMod\):
    //    ParryMod.dll
    //    Assets\Parry\          parry kareleri (yer, duvar, hava, büyük parry, efekt)
    //    Assets\Sounds\         parry sesleri
    //    Assets\ShinobiCharm\   tılsım simgesi, uyarı kareleri ve uyarı sesi
    //    Assets\PureVessel\     void atlası ve bağırma kareleri
    //    Assets\Grimm\          Grimm göz parlaması dosyaları
    //    Exports\               test tuşlarının (F2, F3, F4, F6) çıkardığı dosyalar
    //  Mod açılışta, eski yerlerde (klasörün kökünde, Uyari\, VoidGecis\ ...) duran dosyaları kendisi buraya taşır.
    // =====================================================================
    internal static class ParryAssets
    {
        internal static string ModDir => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        internal static string Dir(string sub) { return System.IO.Path.Combine(System.IO.Path.Combine(ModDir, "Assets"), sub); }
        internal static string File(string sub, string name) { return System.IO.Path.Combine(Dir(sub), name); }



    }


    // =====================================================================
    //  Grimm'in parlayan gözleri: kullanıcının boyadığı atlas kopyaları.
    //  Dosyalar (mod klasöründe ya da GrimmExport klasöründe):
    //    Grimm_Cln_0_glow.png, Grimm_Cln_1_glow.png, Nightmare_Grimm_Cln_0_glow.png
    //  Hızlanma başlayınca yumuşakça bu resimlere geçilir, bitince yavaşça eski haline söner.
    //  (Pure Vessel'in void görünümüyle aynı yöntem: geçişte üstte saydamlaşan kopya, tam halde doğrudan resim değişimi.)
    // =====================================================================
    internal class GrimmGlowSkin : MonoBehaviour
    {
        private const float FadeIn = 0.35f;     // parlamanın gelme süresi
        private const float FadeOut = 2.5f;     // hızlanma bitince sönme süresi
        public float target;
        private float _level;
        private MeshFilter _src, _dst;
        private MeshRenderer _srcR, _dstR;
        private GameObject _ov;
        private MaterialPropertyBlock _mpb;
        private bool _direct;
        private Texture _directTex;
        private readonly System.Collections.Generic.Dictionary<Texture, Texture> _glowFor = new System.Collections.Generic.Dictionary<Texture, Texture>();
        private readonly System.Collections.Generic.Dictionary<Texture, Material> _ovMat = new System.Collections.Generic.Dictionary<Texture, Material>();
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _loaded = new System.Collections.Generic.Dictionary<string, Texture2D>();
        private tk2dSprite _spr;
        // Sadece yüzü (gözleri) görünen kareler parlayan resimle çizilir. Diğerleri (patlama, ışınlanma, alev efektleri...)
        // hep oyunun kendi resmiyle: bazıları farklı şekilde çizilen efekt kareleri, kopyayla çizilince kare kutu görünüyordu.
        private static readonly System.Collections.Generic.HashSet<string> EyeFrames = new System.Collections.Generic.HashSet<string>
        {
            "Grimm Cln/Grimm_Boss_ground_spike_attack0003",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0004",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0005",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0006",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0007",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0008",
            "Grimm Cln/Grimm_Boss_ground_spike_attack0009",
            "Grimm Cln/Grimm_air_dash0000",
            "Grimm Cln/Grimm_air_dash0001",
            "Grimm Cln/Grimm_air_dash0002",
            "Grimm Cln/Grimm_air_dash0003",
            "Grimm Cln/Grimm_air_dash0004",
            "Grimm Cln/Grimm_air_dash0005",
            "Grimm Cln/Grimm_air_dash0006",
            "Grimm Cln/Grimm_air_dash0007",
            "Grimm Cln/Grimm_air_dash0008",
            "Grimm Cln/Grimm_air_dash0009",
            "Grimm Cln/Grimm_air_dash0010",
            "Grimm Cln/Grimm_air_dash0011",
            "Grimm Cln/Grimm_air_dash0012",
            "Grimm Cln/Grimm_air_dash0013",
            "Grimm Cln/Grimm_air_dash0014",
            "Grimm Cln/Grimm_air_dash0015",
            "Grimm Cln/Grimm_air_dash0016",
            "Grimm Cln/Grimm_air_dash0017",
            "Grimm Cln/Grimm_anims_v050000",
            "Grimm Cln/Grimm_anims_v050001",
            "Grimm Cln/Grimm_anims_v050002",
            "Grimm Cln/Grimm_anims_v050003",
            "Grimm Cln/Grimm_anims_v050004",
            "Grimm Cln/Grimm_boss_balloon_attack0000",
            "Grimm Cln/Grimm_boss_balloon_attack0001",
            "Grimm Cln/Grimm_boss_balloon_attack0002",
            "Grimm Cln/Grimm_boss_balloon_attack0003",
            "Grimm Cln/Grimm_boss_balloon_attack0004",
            "Grimm Cln/Grimm_bow0000",
            "Grimm Cln/Grimm_bow0001",
            "Grimm Cln/Grimm_bow0002",
            "Grimm Cln/Grimm_bow0003",
            "Grimm Cln/Grimm_bow0004",
            "Grimm Cln/Grimm_bow0005",
            "Grimm Cln/Grimm_bow0006",
            "Grimm Cln/Grimm_cast0000",
            "Grimm Cln/Grimm_cast0001",
            "Grimm Cln/Grimm_cast0002",
            "Grimm Cln/Grimm_cast0003",
            "Grimm Cln/Grimm_cast0004",
            "Grimm Cln/Grimm_cast0005",
            "Grimm Cln/Grimm_cast0006",
            "Grimm Cln/Grimm_cast0007",
            "Grimm Cln/Grimm_death_stun0000",
            "Grimm Cln/Grimm_death_stun0001",
            "Grimm Cln/Grimm_evade0001",
            "Grimm Cln/Grimm_evade0002",
            "Grimm Cln/Grimm_evade0003",
            "Grimm Cln/Grimm_evade0004",
            "Grimm Cln/Grimm_evade0005",
            "Grimm Cln/Grimm_flame_pillar0000",
            "Grimm Cln/Grimm_flame_pillar0001",
            "Grimm Cln/Grimm_flame_pillar0002",
            "Grimm Cln/Grimm_flame_pillar0003",
            "Grimm Cln/Grimm_flame_pillar0005",
            "Grimm Cln/Grimm_flame_pillar0006",
            "Grimm Cln/Grimm_flame_pillar0007",
            "Grimm Cln/Grimm_flame_pillar0008",
            "Grimm Cln/Grimm_flame_pillar0009",
            "Grimm Cln/Grimm_flame_pillar0010",
            "Grimm Cln/Grimm_flame_pillar0011",
            "Grimm Cln/Grimm_flame_pillar0012",
            "Grimm Cln/Grimm_flame_pillar0014",
            "Grimm Cln/Grimm_idle0000",
            "Grimm Cln/Grimm_idle0001",
            "Grimm Cln/Grimm_idle0002",
            "Grimm Cln/Grimm_idle0003",
            "Grimm Cln/Grimm_idle0004",
            "Grimm Cln/Grimm_idle0005",
            "Grimm Cln/Grimm_idle0006",
            "Grimm Cln/Grimm_idle0007",
            "Grimm Cln/Grimm_idle0008",
            "Grimm Cln/Grimm_idle0009",
            "Grimm Cln/Grimm_idle0010",
            "Grimm Cln/Grimm_idle0011",
            "Grimm Cln/Grimm_idle_right0000",
            "Grimm Cln/Grimm_idle_right0001",
            "Grimm Cln/Grimm_idle_right0002",
            "Grimm Cln/Grimm_idle_right0003",
            "Grimm Cln/Grimm_idle_right0004",
            "Grimm Cln/Grimm_idle_right0005",
            "Grimm Cln/Grimm_idle_right0006",
            "Grimm Cln/Grimm_idle_right0007",
            "Grimm Cln/Grimm_idle_right0008",
            "Grimm Cln/Grimm_idle_right0009",
            "Grimm Cln/Grimm_idle_right0010",
            "Grimm Cln/Grimm_idle_right0011",
            "Grimm Cln/Grimm_idle_right0012",
            "Grimm Cln/Grimm_roar0000",
            "Grimm Cln/Grimm_roar0001",
            "Grimm Cln/Grimm_roar0002",
            "Grimm Cln/Grimm_roar0003",
            "Grimm Cln/Grimm_slash0000",
            "Grimm Cln/Grimm_slash0001",
            "Grimm Cln/Grimm_slash0002",
            "Grimm Cln/Grimm_slash0003",
            "Grimm Cln/Grimm_slash_antic0000",
            "Grimm Cln/Grimm_slash_antic0001",
            "Grimm Cln/Grimm_slash_antic0002",
            "Grimm Cln/Grimm_slash_antic0003",
            "Grimm Cln/Grimm_stun_explode0000",
            "Grimm Cln/Grimm_stun_explode0001",
            "Grimm Cln/Grimm_teleport0003",
            "Grimm Cln/Grimm_teleport0004",
            "Grimm Cln/Grimm_teleport0005",
            "Grimm Cln/Grimm_teleport0006",
            "Grimm Cln/grimm_boss_flame_pillar_talk_loop0002",
            "Grimm Cln/grimm_boss_flame_pillar_talk_loop0003",
            "Nightmare Grimm Cln/Grimm_Boss_ground_spike_attack0003",
            "Nightmare Grimm Cln/Grimm_Boss_ground_spike_attack0004",
            "Nightmare Grimm Cln/Grimm_Boss_ground_spike_attack0005",
            "Nightmare Grimm Cln/Grimm_Boss_ground_spike_attack0006",
            "Nightmare Grimm Cln/Grimm_air_dash0000",
            "Nightmare Grimm Cln/Grimm_air_dash0001",
            "Nightmare Grimm Cln/Grimm_air_dash0002",
            "Nightmare Grimm Cln/Grimm_air_dash0003",
            "Nightmare Grimm Cln/Grimm_air_dash0004",
            "Nightmare Grimm Cln/Grimm_air_dash0005",
            "Nightmare Grimm Cln/Grimm_air_dash0006",
            "Nightmare Grimm Cln/Grimm_air_dash0007",
            "Nightmare Grimm Cln/Grimm_air_dash0008",
            "Nightmare Grimm Cln/Grimm_air_dash0009",
            "Nightmare Grimm Cln/Grimm_air_dash0010",
            "Nightmare Grimm Cln/Grimm_air_dash0011",
            "Nightmare Grimm Cln/Grimm_anims_v050000",
            "Nightmare Grimm Cln/Grimm_anims_v050001",
            "Nightmare Grimm Cln/Grimm_anims_v050002",
            "Nightmare Grimm Cln/Grimm_anims_v050003",
            "Nightmare Grimm Cln/Grimm_anims_v050004",
            "Nightmare Grimm Cln/Grimm_boss_balloon_attack0000",
            "Nightmare Grimm Cln/Grimm_boss_balloon_attack0001",
            "Nightmare Grimm Cln/Grimm_boss_balloon_attack0002",
            "Nightmare Grimm Cln/Grimm_boss_balloon_attack0003",
            "Nightmare Grimm Cln/Grimm_boss_balloon_attack0004",
            "Nightmare Grimm Cln/Grimm_evade0001",
            "Nightmare Grimm Cln/Grimm_evade0002",
            "Nightmare Grimm Cln/Grimm_evade0003",
            "Nightmare Grimm Cln/Grimm_evade0004",
            "Nightmare Grimm Cln/Grimm_evade0005",
            "Nightmare Grimm Cln/Grimm_idle0000",
            "Nightmare Grimm Cln/Grimm_idle0001",
            "Nightmare Grimm Cln/Grimm_idle0002",
            "Nightmare Grimm Cln/Grimm_idle0003",
            "Nightmare Grimm Cln/Grimm_slash0000",
            "Nightmare Grimm Cln/Grimm_slash0001",
            "Nightmare Grimm Cln/Grimm_slash0002",
            "Nightmare Grimm Cln/Grimm_slash0003",
            "Nightmare Grimm Cln/Grimm_slash_antic0000",
            "Nightmare Grimm Cln/Grimm_slash_antic0001",
            "Nightmare Grimm Cln/Grimm_slash_antic0002",
            "Nightmare Grimm Cln/Grimm_slash_antic0003",
            "Nightmare Grimm Cln/Grimm_stun_explode0000",
            "Nightmare Grimm Cln/Grimm_stun_explode0001",
            "Nightmare Grimm Cln/Grimm_stun_extra_frames0000",
            "Nightmare Grimm Cln/Grimm_stun_extra_frames0001",
            "Nightmare Grimm Cln/Grimm_stun_extra_frames0002",
            "Nightmare Grimm Cln/Grimm_teleport0003",
            "Nightmare Grimm Cln/Grimm_teleport0004",
            "Nightmare Grimm Cln/Grimm_teleport0005",
            "Nightmare Grimm Cln/Grimm_teleport0006",
            "Nightmare Grimm Cln/temp0003",
            "Nightmare Grimm Cln/temp0004",
            "Nightmare Grimm Cln/temp0005",
            "Nightmare Grimm Cln/temp0006",
            "Nightmare Grimm Cln/temp0007"
        };

        internal static GrimmGlowSkin For(GameObject boss, ParryMod mod)
        {
            if (boss == null) return null;
            GrimmGlowSkin g = boss.GetComponent<GrimmGlowSkin>();
            if (g != null) return g;
            g = boss.AddComponent<GrimmGlowSkin>();
            int n = g.Build(mod);
            if (n == 0) {  Destroy(g); return null; }
            return g;
        }

        // Dışa aktarmadaki sırayla aynı: koleksiyondaki karelerin resimleri ilk görülme sırasına göre _0, _1 ...
        private int Build(ParryMod mod)
        {
            _src = GetComponent<MeshFilter>(); _srcR = GetComponent<MeshRenderer>(); _spr = GetComponent<tk2dSprite>();
            tk2dSpriteAnimator an = GetComponent<tk2dSpriteAnimator>();
            if (_src == null || _srcR == null || an == null || an.Library == null) return 0;
            string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var cols = new System.Collections.Generic.List<tk2dSpriteCollectionData>();
            foreach (var c in an.Library.clips)
                if (c != null && c.frames != null) foreach (var f in c.frames) if (f != null && f.spriteCollection != null && !cols.Contains(f.spriteCollection)) cols.Add(f.spriteCollection);
            foreach (var col in cols)
            {
                var order = new System.Collections.Generic.List<Texture>();
                foreach (var def in col.spriteDefinitions)
                {
                    if (def == null || def.material == null || def.positions == null || def.uvs == null) continue;
                    Texture tex = def.material.mainTexture;
                    if (tex == null || order.Contains(tex)) continue;
                    order.Add(tex);
                    string name = Safe(col.spriteCollectionName) + "_" + (order.Count - 1) + "_glow.png";
                    Texture2D glow = LoadGlow(dir, name, tex, mod);
                    if (glow != null) _glowFor[tex] = glow;
                }
            }
            return _glowFor.Count;
        }

        private static Texture2D LoadGlow(string dir, string name, Texture orig, ParryMod mod)
        {
            if (_loaded.TryGetValue(name, out Texture2D t) && t != null) return t;
            string path = ParryAssets.File("Grimm", name);
            if (!System.IO.File.Exists(path)) return null;
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(System.IO.File.ReadAllBytes(path));
            if (t.width != orig.width || t.height != orig.height)
            {
                Destroy(t); return null;
            }
            t.filterMode = orig.filterMode; t.wrapMode = TextureWrapMode.Clamp;
            _loaded[name] = t;
            return t;
        }

        private static string Safe(string s)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }

        private Texture CurrentOrigTex()
        {
            if (_srcR == null || _srcR.sharedMaterial == null) return null;
            return _srcR.sharedMaterial.mainTexture;
        }

        private void SetTex(Texture t)
        {
            if (_srcR == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _srcR.GetPropertyBlock(_mpb);   // oyunun koyduğu değerler (vurulma parlaması vb.) korunur
            if (t != null) _mpb.SetTexture("_MainTex", t); else _mpb.Clear();
            _srcR.SetPropertyBlock(_mpb);
        }

        void LateUpdate()
        {
            _level = Mathf.MoveTowards(_level, target, Time.deltaTime / (target > _level ? FadeIn : FadeOut));
            Texture orig = CurrentOrigTex();
            Texture glow = orig != null && _glowFor.TryGetValue(orig, out Texture g) ? g : null;
            if (glow != null && _spr != null)
            {
                var def = _spr.CurrentSprite;
                string key = (_spr.Collection != null ? _spr.Collection.spriteCollectionName : "") + "/" + (def != null ? def.name : "");
                if (!EyeFrames.Contains(key)) glow = null;   // yüzsüz kare: oyunun kendi resmi
            }
            if (_level >= 0.999f && glow != null)
            {
                // Tam parlak: Grimm doğrudan senin resminle çizilir (her karede yeniden, çünkü kare değişince resim de değişebilir).
                SetTex(glow); _direct = true; _directTex = glow;
                if (_ov != null) _ov.SetActive(false);
                return;
            }
            if (_direct) { SetTex(orig); _direct = false; _directTex = null; }   // eski resme dön
            if (_level <= 0.001f || glow == null) { if (_ov != null) _ov.SetActive(false); return; }
            // Geçiş: üstte, aynı şekli parlayan resimle çizen saydam kopya.
            if (_ov == null)
            {
                _ov = new GameObject("Grimm Glow Skin (ParryMod)");
                _ov.transform.SetParent(transform, false);
                _ov.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                _ov.layer = gameObject.layer;
                _dst = _ov.AddComponent<MeshFilter>();
                _dstR = _ov.AddComponent<MeshRenderer>();
            }
            _ov.SetActive(true);
            if (!_ovMat.TryGetValue(glow, out Material m) || m == null)
            {
                m = new Material(Shader.Find("Sprites/Default")); m.mainTexture = glow;
                _ovMat[glow] = m;
            }
            _dstR.sharedMaterial = m;
            if (_dst.sharedMesh != _src.sharedMesh) _dst.sharedMesh = _src.sharedMesh;
            _dstR.enabled = _srcR.enabled;
            _dstR.sortingLayerID = _srcR.sortingLayerID; _dstR.sortingOrder = _srcR.sortingOrder + 1;
            m.color = new Color(1f, 1f, 1f, _level);
        }

        void OnDestroy()
        {
            if (_direct) SetTex(null);
            if (_ov != null) Destroy(_ov);
            foreach (var m in _ovMat.Values) if (m != null) Destroy(m);
        }
    }
}
