using UnityEngine;

namespace Ghosts
{
    public sealed class StoryAudio : MonoBehaviour
    {
        AudioSource current, outgoing, effects;
        string track;
        float fade, volume;

        void Awake()
        {
            current = MakeSource(true);
            outgoing = MakeSource(true);
            effects = MakeSource(false);
        }

        AudioSource MakeSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }

        public void SetVolume(float music, float sfx)
        {
            volume = music;
            effects.volume = sfx;
        }

        public void PlayTrack(string id)
        {
            if (id == track) return;
            track = id;
            (current, outgoing) = (outgoing, current);
            current.Stop();
            current.clip = string.IsNullOrEmpty(id) ? null : Resources.Load<AudioClip>("Audio/" + id);
            fade = 0;
            current.volume = 0;
            if (current.clip != null) current.Play();
        }

        public void PlayEffect(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var clip = Resources.Load<AudioClip>("Audio/" + id);
            if (clip != null) effects.PlayOneShot(clip);
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, 1, Time.unscaledDeltaTime / .65f);
            current.volume = fade * volume;
            outgoing.volume = (1 - fade) * volume;
            if (fade == 1 && outgoing.isPlaying) outgoing.Stop();
        }
    }
}
