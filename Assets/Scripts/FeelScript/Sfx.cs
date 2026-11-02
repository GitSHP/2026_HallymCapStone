using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.Feel
{
    public class Sfx : MonoBehaviour    // 게임 코드와 효과음 재생을 잇는 유일한 연결 지점 (Fx의 소리 버전)
    // -> 게임 코드는 AudioSource를 직접 만지지 않고 Sfx.Play(SfxId.Move)처럼 이 함수만 부름
    // -> 처음 소리를 낼 때 씬이 바뀌어도 사라지지 않는 "Sfx" 오브젝트를 하나 만들고, AudioSource 여러 개를 돌려 써서 소리가 겹쳐도 끊기지 않게 함
    // -> UI 클릭음도 여기서 처리함: 누를 수 있는 UI를 누르는 순간 클릭음을 냄 (버튼마다 따로 연결하지 않음)
    {
        const int VoiceCount = 10;  // 동시에 낼 수 있는 소리 수

        static Sfx _instance;   // 씬이 바뀌어도 유지되는 효과음 담당 오브젝트
        static SfxLibrary _library; // 불러온 효과음 설정 에셋

        readonly List<AudioSource> _voices = new();  // 돌려 쓰는 AudioSource 목록
        int _next;  // 다음에 쓸 AudioSource 번호
        AudioListener _listener;    // 씬에 소리를 들을 귀(AudioListener)가 없을 때 대신 쓰는 귀

        static SfxLibrary Library   // 효과음 설정 에셋 -> Resources 폴더에서 한 번만 불러옴
        {
            get
            {
                if (_library == null)
                    _library = Resources.Load<SfxLibrary>("SfxLibrary");
                return _library;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()  // 게임이 시작되면 효과음 담당 오브젝트를 미리 만들어 둠 -> 첫 화면(타이틀)의 UI 클릭음도 들리게
        {
            Ensure();
        }

        static Sfx Ensure() // 효과음 담당 오브젝트가 없으면 만드는 함수
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Sfx>();
            return _instance;
        }

        public static void Play(SfxId id)   // 효과음 하나를 재생하는 함수 -> 후보 소리 중 하나를 골라 음높이를 살짝 흔들어 재생
        {
            var entry = Library != null ? Library.Find(id) : null;
            if (entry == null || entry.clips == null || entry.clips.Length == 0)    // 설정이 없으면 조용히 넘어감 -> 소리가 없어도 게임은 돌아감
                return;

            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null)
                return;

            Ensure().PlayClip(clip, entry.volume * Library.masterVolume, Random.Range(entry.pitch.x, entry.pitch.y));
        }

        void Awake()
        {
            for (int i = 0; i < VoiceCount; i++)    // AudioSource를 미리 만들어 둠
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;   // 2D 소리 -> 카메라 위치와 상관없이 같은 크기로 들림
                _voices.Add(source);
            }

            _listener = gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            UpdateListener();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateListener();
        }

        void UpdateListener()   // 씬에 다른 귀(카메라의 AudioListener)가 있으면 내 귀는 끄고, 없으면 켬 -> 귀가 둘이면 유니티가 경고를 띄움
        {
            bool other = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener != _listener && listener.isActiveAndEnabled)
                    other = true;
            _listener.enabled = !other;
        }

        void PlayClip(AudioClip clip, float volume, float pitch)    // 쉬고 있는 AudioSource를 골라 소리를 내는 함수 -> 모두 바쁘면 가장 오래된 소리를 끊고 씀
        {
            AudioSource source = null;
            for (int i = 0; i < _voices.Count; i++)
            {
                var candidate = _voices[(_next + i) % _voices.Count];
                if (!candidate.isPlaying)
                {
                    source = candidate;
                    break;
                }
            }
            if (source == null)
                source = _voices[_next];
            _next = (_voices.IndexOf(source) + 1) % _voices.Count;

            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        // ---------- UI 클릭음 ----------
        static readonly List<RaycastResult> Hits = new();   // UI 레이캐스트 결과를 잠시 담는 목록 -> 매번 새로 만들지 않고 재사용

        void Update()   // 매 프레임 포인터를 누른 순간인지 확인하고, 누른 곳이 누를 수 있는 UI라면 클릭음을 냄
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || EventSystem.current == null)
                return;

            var data = new PointerEventData(EventSystem.current) { position = pointer.position.ReadValue() };
            Hits.Clear();
            EventSystem.current.RaycastAll(data, Hits);
            if (Hits.Count == 0)
                return;

            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(Hits[0].gameObject);  // 가장 위에 있는 UI에서 클릭을 받는 오브젝트를 찾음
            if (handler == null)    // 누를 수 없는 UI(배경, 글자 등)라면 소리 없음
                return;

            var selectable = handler.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) // 꺼진 유니티 버튼이라면 소리 없음
                return;

            Play(SfxId.UiClick);
        }
    }
}
