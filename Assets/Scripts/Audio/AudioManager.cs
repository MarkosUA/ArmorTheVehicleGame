using System.Collections.Generic;
using UnityEngine;

namespace ArmorTheVehicle.Audio
{
    /// Background music playlist plus one-shot gameplay SFX. Picks a random track on start,
    /// and whenever one finishes plays another random track, for as long as the track list
    /// isn't empty. Every clip — music or SFX — is optional: with nothing assigned in the
    /// Inspector this component plays silently and never throws, so audio can be wired up
    /// incrementally without breaking anything in the meantime.
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private List<AudioClip> _musicTracks = new();

        [Header("SFX")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _buttonClickSfx;
        [SerializeField] private AudioClip _shootSfx;
        [SerializeField] private AudioClip _enemyAttackSfx;
        [SerializeField] private AudioClip _enemyHitSfx;
        [SerializeField] private AudioClip _victorySfx;
        [SerializeField] private AudioClip _defeatSfx;

        private int _lastTrackIndex = -1;

        private void Start()
        {
            PlayRandomTrack();
        }

        private void Update()
        {
            // No loop flag on the source: once a track finishes, isPlaying drops to false
            // and this picks the next one — the "then randomly pick the next, and so on"
            // playlist behavior.
            if (_musicSource != null && _musicTracks.Count > 0 && !_musicSource.isPlaying)
            {
                PlayRandomTrack();
            }
        }

        private void PlayRandomTrack()
        {
            if (_musicSource == null || _musicTracks.Count == 0) return;

            int index = Random.Range(0, _musicTracks.Count);
            if (_musicTracks.Count > 1 && index == _lastTrackIndex)
            {
                index = (index + 1) % _musicTracks.Count; // avoid repeating the same track twice in a row
            }
            _lastTrackIndex = index;

            AudioClip track = _musicTracks[index];
            if (track == null) return; // tolerate an empty slot in the list rather than erroring

            _musicSource.clip = track;
            _musicSource.Play();
        }

        public void PlayButtonClick() => PlaySfx(_buttonClickSfx);
        public void PlayShoot() => PlaySfx(_shootSfx);
        public void PlayEnemyAttack() => PlaySfx(_enemyAttackSfx);
        public void PlayEnemyHit() => PlaySfx(_enemyHitSfx);
        public void PlayVictory() => PlaySfx(_victorySfx);
        public void PlayDefeat() => PlaySfx(_defeatSfx);

        private void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
