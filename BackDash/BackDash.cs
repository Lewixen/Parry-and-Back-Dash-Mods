using System;
using Modding;
using UnityEngine;

namespace BackDash
{
    // Geriye dash: arkana döndüğün anda (ters yöne bastığın an) dash tuşuna basarsan Knight dönmeden,
    // eski baktığı yönü koruyarak geriye dash atar. Yerde ve havada çalışır.
    // Oyunun kendi dash'i kullanılır, sadece yönü ters çevrilir. Bu yüzden mesafe, hız, bekleme süresi,
    // Shade Cloak (gölge dash'i ve hasar almama) normal dash'le tamamen aynı ve bekleme süreleri ortak.
    // Her kamera çiziminden hemen önce: oyunun o an gösterdiği gölge dash çizimine bakar, ilk 3'üyse yerine bizimkini koyar.
    public class ShadeSwap : MonoBehaviour
    {
        private tk2dSprite _ks; private MeshRenderer _kr; private SpriteRenderer _sr; private Sprite[] _shade, _dash;
        private bool _on;
        public void Init(tk2dSprite ks, MeshRenderer kr, SpriteRenderer sr, Sprite[] shade, Sprite[] dash)
        {
            _ks = ks; _kr = kr; _sr = sr; _shade = shade; _dash = dash; _on = true;
            Camera.onPreCull += Apply;
        }
        public void Stop()
        {
            if (!_on) return;
            _on = false;
            Camera.onPreCull -= Apply;
            if (_kr != null) _kr.enabled = true;
        }
        private void OnDestroy() { Stop(); }
        private void Apply(Camera cam)
        {
            if (!_on || _sr == null) return;
            string cur = null;
            try { cur = _ks != null && _ks.CurrentSprite != null ? _ks.CurrentSprite.name : null; } catch { }
            Sprite pick = null;
            int si = BackDashMod.ShadeIndexFor(cur), di = BackDashMod.DashIndexFor(cur);
            if (_shade != null && si >= 0 && si < _shade.Length) pick = _shade[si];
            else if (_dash != null && di >= 0 && di < _dash.Length) pick = _dash[di];
            if (pick != null)
            {
                _sr.sprite = pick; _sr.enabled = true;
                if (_kr != null) _kr.enabled = false;
            }
            else
            {
                _sr.enabled = false;
                if (_kr != null) _kr.enabled = true;
            }
        }
    }

    // Dönme geçişi: süre boyunca Knight'ın çizimi yerine oyunun dönme kareleri gösterilir.
    public class TurnSwap : MonoBehaviour
    {
        private HeroController _h; private MeshRenderer _kr; private tk2dSprite _ov; private int _a, _b;
        private float _t0, _dur; private bool _on;
        public void Init(HeroController h, MeshRenderer kr, tk2dSprite ov, int a, int b, float dur)
        {
            _h = h; _kr = kr; _ov = ov; _a = a; _b = b; _dur = dur; _t0 = Time.time; _on = true;
            Camera.onPreCull += Apply;
        }
        public void Stop()
        {
            if (!_on) return;
            _on = false;
            Camera.onPreCull -= Apply;
            if (_kr != null) _kr.enabled = true;
            if (this != null && gameObject != null) Destroy(gameObject);
        }
        private void OnDestroy() { if (_on) { _on = false; Camera.onPreCull -= Apply; if (_kr != null) _kr.enabled = true; } }
        private void Apply(Camera cam)
        {
            if (!_on) return;
            float k = (Time.time - _t0) / _dur;
            // Süre bitti ya da başka bir şey yapılıyor (saldırı, büyü, hasar, dash): geçiş biter.
            if (_h == null || k >= 1f || _h.cState.attacking || _h.cState.dashing || _h.cState.recoiling || _h.cState.casting || _h.cState.dead)
            { Stop(); return; }
            _ov.SetSprite(k < 0.5f ? _a : _b);
            if (_kr != null) _kr.enabled = false;
        }
    }

    public class BackDashMod : Mod
    {
        public BackDashMod() : base("Back Dash") { }
        public override string GetVersion() => "1.0.0.0";

        private bool _backDash;          // şu anki dash geriye mi

        public override void Initialize()
        {
            On.HeroController.Update += OnHeroUpdate;
            On.HeroController.HeroDash += OnHeroDash;
            On.HeroController.Dash += OnDash;
            On.HeroController.FinishedDashing += OnFinishedDashing;
            On.CameraTarget.Update += OnCameraUpdate;
        }





        // Arkana dönme anı: baktığın yönün tersine bastığın kare. Dash tuşu bu andan sonra kısa süre içinde
        // basılırsa (aynı anda basmak zor olduğu için küçük bir pay) geriye dash olur.
        private const float TurnWindow = 0.15f;
        private float _turnTime = -10f;
        private bool _facingBeforeTurn;

        private void OnHeroUpdate(On.HeroController.orig_Update orig, HeroController self)
        {
            try
            {
                var ia = InputHandler.Instance != null ? InputHandler.Instance.inputActions : null;
                if (ia != null && !self.cState.dashing)
                {
                    bool pressRight = ia.right.WasPressed, pressLeft = ia.left.WasPressed;
                    if ((pressRight && !self.cState.facingRight) || (pressLeft && self.cState.facingRight))
                    {
                        _turnTime = Time.time;
                        _facingBeforeTurn = self.cState.facingRight;
                    }
                }
            }
            catch (Exception e) { Log("Hata (dönme okuma): " + e.Message); }
            bool faceBefore = self.cState.facingRight;
            orig(self);
            // Dönme geçişi sadece Knight GERÇEKTEN döndüyse (oturma, konuşma, harita gibi kontrolün kapalı olduğu
            // anlarda tuşa basınca Knight dönmüyor; orada oynarsa animasyon bozuluyordu).
            if (TurnFrames && self.cState.facingRight != faceBefore && Time.time == _turnTime
                && self.acceptingInput && !self.controlReqlinquished && !self.cState.dashing)
                StartTurnFrames(self);
        }

        // ---------- Dönme geçişi (deneme) ----------
        // Arkana döndüğün an, geriye dash payı (0.15 sn) boyunca ekranda oyunun kendi dönme kareleri
        // (turn001 -> turn000) oynar. Kontrol gecikmez, sadece görüntü. Dash'e basarsan geçiş hemen biter.
        private const bool TurnFrames = true;   // false: kapalı (eski hali)
        private TurnSwap _turnSwap;

        private void StartTurnFrames(HeroController self)
        {
            try
            {
                if (!CanDashNow(self)) return;   // dash beklemedeyse geriye dash olamaz: normal anında dönme
                tk2dSprite ks = self.GetComponent<tk2dSprite>();
                if (ks == null || ks.Collection == null) return;
                int a = ks.Collection.GetSpriteIdByName("turn001", -1), b = ks.Collection.GetSpriteIdByName("turn000", -1);
                if (a < 0 || b < 0) {  return; }
                if (_turnSwap != null) _turnSwap.Stop();
                GameObject go = new GameObject("Turn Frames (BackDash)");
                go.transform.SetParent(self.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                tk2dSprite ov = tk2dSprite.AddComponent(go, ks.Collection, a);
                MeshRenderer kr = self.GetComponent<MeshRenderer>();
                MeshRenderer ovr = go.GetComponent<MeshRenderer>();
                if (kr != null && ovr != null) { ovr.sortingLayerID = kr.sortingLayerID; ovr.sortingOrder = kr.sortingOrder; }
                _turnSwap = go.AddComponent<TurnSwap>();
                _turnSwap.Init(self, kr, ov, a, b, TurnWindow);
            }
            catch (Exception e) { Log("Hata (dönme kareleri): " + e.Message); }
        }

        private static System.Reflection.MethodInfo _canDash;
        private static bool CanDashNow(HeroController h)
        {
            try
            {
                if (_canDash == null) _canDash = typeof(HeroController).GetMethod("CanDash", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                return _canDash == null || (bool)_canDash.Invoke(h, null);
            }
            catch { return true; }
        }

        // Dash başlarken: az önce arkana döndüysen bu dash geriye olacak.
        private void OnHeroDash(On.HeroController.orig_HeroDash orig, HeroController self)
        {
            bool back = false;
            bool faceRight = self.cState.facingRight;
            try
            {
                var ia = InputHandler.Instance != null ? InputHandler.Instance.inputActions : null;
                bool holdingBack = ia != null && (_facingBeforeTurn ? ia.left.IsPressed : ia.right.IsPressed);
                back = Time.time - _turnTime <= TurnWindow && holdingBack;
                if (back) faceRight = _facingBeforeTurn;   // dönmeden önceki yön korunur
            }
            catch (Exception e) { Log("Hata (tuş okuma): " + e.Message); }

            orig(self);
            if (_turnSwap != null) { _turnSwap.Stop(); _turnSwap = null; }   // dash başladı: dönme geçişi biter
            _backDash = back && self.cState.dashing && !IsDashingDown(self);
            if (_backDash)
            {
                // Oyun dash başlarken yön tuşuna göre döndürebiliyor: geriye dash'te baktığın yön değişmesin.
                if (self.cState.facingRight != faceRight) { if (faceRight) self.FaceRight(); else self.FaceLeft(); }
                if (ShowBetaEffect) SpawnBetaEffect(self);
                StartShadeFrames(self);   // gölge ya da normal: hangi çizim gösteriliyorsa ona göre değişir
            }
        }

        // Oyun dash sırasında her fizik adımında hızı baktığın yöne verir: geriye dash'te ters çeviriyoruz.
        private void OnDash(On.HeroController.orig_Dash orig, HeroController self)
        {
            orig(self);
            if (!_backDash) return;
            if (!self.cState.dashing) { _backDash = false; return; }
            Rigidbody2D rb = self.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            float speed = Mathf.Abs(rb.velocity.x);
            float dir = self.cState.facingRight ? -1f : 1f;   // baktığın yönün tersi
            rb.velocity = new Vector2(dir * speed, rb.velocity.y);
        }

        // ---------- Geriye gölge dash'inin kendi çizimleri ----------
        // Mod klasöründeki BackShade_1.png ... BackShade_7.png, oyunun gölge dash animasyonunun 7 çiziminin yerine geçer
        // (shadow_dash0001-0007; Sharp Shadow'da onun 2-5. çizimleri de). Eksik olan kareler oyunun kendi çizimiyle kalır.
        // Çizimler sağa bakıyor; Knight hangi yöne bakıyorsa ona göre çevrilir.
        private const float ShadeFramePPU = 76f;      // çizimin büyüklüğü: sayı küçüldükçe Knight büyür
        private static readonly Vector2[] ShadePivots =   // her karenin Knight'ın merkezine denk gelen noktası (0-1)
        {
            new Vector2(0.542f, 0.46f), new Vector2(0.694f, 0.444f), new Vector2(0.735f, 0.437f),
            new Vector2(0.672f, 0.446f), new Vector2(0.649f, 0.506f), new Vector2(0.654f, 0.465f), new Vector2(0.515f, 0.458f)
        };
        // Oyunun çizim adı -> bizim kare
        internal static int ShadeIndexFor(string spriteName)
        {
            if (spriteName == null) return -1;
            switch (spriteName)
            {
                case "shadow_dash0001": return 0;
                case "shadow_dash0002": case "sharp_shade_dash_front0002": return 1;
                case "shadow_dash0003": case "sharp_shade_dash_front0003": return 2;
                case "shadow_dash0004": case "sharp_shade_dash_front0004": return 3;
                case "shadow_dash0005": case "sharp_shade_dash_front0005": return 4;
                case "shadow_dash0006": return 5;
                case "shadow_dash0007": return 6;
            }
            return -1;
        }
        // Normal geriye dash: mod klasöründeki BackDash_1.png ... BackDash_7.png, oyunun dash animasyonunun
        // 7 çiziminin (dash_v020001-0007) yerine geçer.
        private static readonly Vector2[] DashPivots =
        {
            new Vector2(0.595f, 0.541f), new Vector2(0.733f, 0.504f), new Vector2(0.601f, 0.507f), new Vector2(0.659f, 0.5f),
            new Vector2(0.599f, 0.483f), new Vector2(0.575f, 0.5f), new Vector2(0.554f, 0.489f)
        };
        internal static int DashIndexFor(string spriteName)
        {
            if (spriteName == null || !spriteName.StartsWith("dash_v02000")) return -1;
            int n;
            if (!int.TryParse(spriteName.Substring("dash_v02000".Length), out n)) return -1;
            return n >= 1 && n <= 7 ? n - 1 : -1;
        }

        private Sprite[] _shadeFrames, _dashFrames;
        private bool _shadeLoadTried;

        private void LoadShadeFrames()
        {
            if (_shadeLoadTried) return;
            _shadeLoadTried = true;
            _shadeFrames = LoadSet("BackShade_", ShadePivots, "gölge dash");
            _dashFrames = LoadSet("BackDash_", DashPivots, "normal dash");
        }

        private Sprite[] LoadSet(string prefix, Vector2[] pivots, string what)
        {
            string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 1; i <= 7; i++)
            {
                string path = System.IO.Path.Combine(dir, prefix + i + ".png");
                if (!System.IO.File.Exists(path))
                {
                    break;
                }
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                t.filterMode = FilterMode.Bilinear;
                Vector2 pv = pivots[Mathf.Min(i - 1, pivots.Length - 1)];
                list.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), pv, ShadeFramePPU));
            }
            if (list.Count == 0) return null;
            return list.ToArray();
        }

        private int _shadeToken;
        private void StartShadeFrames(HeroController self)
        {
            LoadShadeFrames();
            if (_shadeFrames == null && _dashFrames == null) return;
            GameManager.instance.StartCoroutine(PlayShadeFrames(self, ++_shadeToken));
        }

        private System.Collections.IEnumerator PlayShadeFrames(HeroController self, int token)
        {
            MeshRenderer knightR = self.GetComponent<MeshRenderer>();
            GameObject go = new GameObject("Back Shade Frames (BackDash)");
            go.transform.SetParent(self.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            // Çizim Knight'la aynı yöne çevrilir: Knight hangi yöne bakıyorsa çizim de o yöne bakar (geriye giderken öne bakar).
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (knightR != null) { sr.sortingLayerID = knightR.sortingLayerID; sr.sortingOrder = knightR.sortingOrder; }
            // Kare seçimi, oyun animasyonu o karenin çizimini koyduktan SONRA ve ekran çizilmeden HEMEN ÖNCE yapılır
            // (yoksa bir an oyunun kendi çizimi görünebilir).
            ShadeSwap swap = go.AddComponent<ShadeSwap>();
            swap.Init(self.GetComponent<tk2dSprite>(), knightR, sr, _shadeFrames, _dashFrames);
            while (self != null && token == _shadeToken && _backDash && self.cState.dashing) yield return null;
            if (swap != null) swap.Stop();
            if (knightR != null) knightR.enabled = true;
            UnityEngine.Object.Destroy(go);
        }




        // Oyunun dosyalarında kalan beta geriye dash efekti ("BackdashTriple Effect").
        private const bool ShowBetaEffect = true;   // false: efekt çıkmaz
        private static System.Reflection.FieldInfo _prefabField;

        private void SpawnBetaEffect(HeroController self)
        {
            try
            {
                if (_prefabField == null)
                    _prefabField = typeof(HeroController).GetField("backDashPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                GameObject prefab = _prefabField != null ? _prefabField.GetValue(self) as GameObject : null;
                if (prefab == null) return;
                GameObject fx = UnityEngine.Object.Instantiate(prefab, self.transform.position, Quaternion.identity);
                Vector3 sc = fx.transform.localScale;
                // Ters yöne çevrilir (önceki yönü yanlıştı).
                sc.x = Mathf.Abs(sc.x) * (self.cState.facingRight ? 1f : -1f);
                fx.transform.localScale = sc;
                fx.SetActive(true);
                UnityEngine.Object.Destroy(fx, 3f);   // kendisi kaybolmazsa diye
            }
            catch (Exception e) { Log("Hata (beta efekti): " + e.Message); }
        }

        private void OnFinishedDashing(On.HeroController.orig_FinishedDashing orig, HeroController self)
        {
            bool wasBack = _backDash;
            orig(self);
            _backDash = false;
            if (wasBack) _turnTime = -10f;
        }

        private static bool IsDashingDown(HeroController h)
        {
            try
            {
                var f = typeof(HeroController).GetField("dashingDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                return f != null && (bool)f.GetValue(h);
            }
            catch { return false; }
        }

        // Kamera dash sırasında Knight'ın BAKTIĞI yöne doğru öne kayıyor; geriye dash'te Knight ters yöne gittiği için
        // kamera onu zor yakalıyordu. Geriye dash sürerken kamera, Knight gittiği yöne bakıyormuş gibi hesaplar
        // (sadece kameranın hesabı için; Knight'ın görüntüsü ve baktığı yön değişmez).
        private void OnCameraUpdate(On.CameraTarget.orig_Update orig, CameraTarget self)
        {
            HeroController h = HeroController.instance;
            bool flip = _backDash && h != null && h.cState.dashing;
            if (flip) h.cState.facingRight = !h.cState.facingRight;
            try { orig(self); }
            finally { if (flip) h.cState.facingRight = !h.cState.facingRight; }
        }

    }
}
