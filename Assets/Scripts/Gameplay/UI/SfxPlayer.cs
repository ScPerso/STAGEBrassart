using UnityEngine;

namespace Magma.Gameplay.UI
{
    /// <summary>
    /// Lecteur audio global pour les effets d'interface (survol, clic). Persiste entre les
    /// changements de scène (DontDestroyOnLoad) afin qu'un son de clic ne soit pas coupé net
    /// lorsque le bouton cliqué déclenche immédiatement un chargement de scène : sans cela, le
    /// son disparaît avec le GameObject du bouton avant même d'avoir pu être entendu.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;

        private AudioSource audioSource;

        /// <summary>Retourne l'instance globale, en la créant si elle n'existe pas encore dans la scène.</summary>
        public static SfxPlayer EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject sfxPlayerObject = new GameObject("SfxPlayer");
            instance = sfxPlayerObject.AddComponent<SfxPlayer>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        /// <summary>Joue un clip sans qu'il soit coupé par un changement de scène survenant juste après l'appel.</summary>
        public void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, volume);
        }
    }
}
