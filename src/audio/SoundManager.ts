export type SoundName = 'coo' | 'flap' | 'peck' | 'step';
/** Register real files later. Unregistered sounds are intentionally silent. */
class SoundManager {
  private sources = new Map<SoundName, string>();
  private playing = new Set<HTMLAudioElement>();
  enabled = false;
  register(name: SoundName, url: string) {
    this.sources.set(name, url);
  }
  play(name: SoundName) {
    const src = this.sources.get(name);
    if (!this.enabled || !src) return;
    const audio = new Audio(src);
    audio.volume = 0.35;
    this.playing.add(audio);
    audio.onended = () => this.playing.delete(audio);
    audio.play().catch(() => this.playing.delete(audio));
  }
  stop() {
    for (const audio of this.playing) {
      audio.pause();
      audio.currentTime = 0;
    }
    this.playing.clear();
  }
}
export const soundManager = new SoundManager();
