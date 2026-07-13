/**
 * Hides how the roll sound effect is produced — a bundled CC0 asset or a synthesized WebAudio
 * rattle are both valid implementations; nothing outside this module can tell which.
 */
export function playRollSound(): void {
  try {
    const AudioContextClass = window.AudioContext || (window as unknown as { webkitAudioContext: typeof window.AudioContext }).webkitAudioContext;
    const audioContext = new AudioContextClass();
    const now = audioContext.currentTime;

    // Create a short noisy rattle: blend of clicking and buzzing
    const osc = audioContext.createOscillator();
    const noise = audioContext.createBufferSource();
    const gainNode = audioContext.createGain();
    const noiseGain = audioContext.createGain();

    // Generate white noise buffer
    const bufferSize = audioContext.sampleRate * 0.1;
    const noiseBuffer = audioContext.createBuffer(1, bufferSize, audioContext.sampleRate);
    const data = noiseBuffer.getChannelData(0);
    for (let i = 0; i < bufferSize; i++) {
      data[i] = Math.random() * 2 - 1;
    }

    // Configure nodes
    osc.frequency.setValueAtTime(600, now);
    osc.frequency.exponentialRampToValueAtTime(300, now + 0.08);

    gainNode.gain.setValueAtTime(0.3, now);
    gainNode.gain.exponentialRampToValueAtTime(0.01, now + 0.1);

    noiseGain.gain.setValueAtTime(0.15, now);
    noiseGain.gain.exponentialRampToValueAtTime(0.01, now + 0.1);

    // Connect and play
    osc.connect(gainNode);
    gainNode.connect(audioContext.destination);

    noise.buffer = noiseBuffer;
    noise.connect(noiseGain);
    noiseGain.connect(audioContext.destination);

    osc.start(now);
    osc.stop(now + 0.1);
    noise.start(now);
    noise.stop(now + 0.1);
  } catch {
    // Silently fail if audio context is not available (testing, etc.)
  }
}
