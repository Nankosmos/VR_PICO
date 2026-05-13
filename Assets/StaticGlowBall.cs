using UnityEngine;

public class StaticGlowBall : MonoBehaviour
{
    [Range(0, 2)] 
    public float freezeTime = 0.5f; // 调整这个数值可以改变光球静止时的形态

    void Start()
    {
        // 1. 获取当前物体（lan）及其所有子物体下的所有粒子系统
        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();

        if (allParticles.Length == 0) return;

        // 2. 让它们同步播放
        foreach (ParticleSystem ps in allParticles)
        {
            ps.Play();
            // 3. 强制模拟到 freezeTime 指定的那一帧，并暂停
            // true 表示冻结在当前模拟状态，false 表示不重置
            ps.Simulate(freezeTime, true, false);
            ps.Pause(); 
        }
        
        // 4. (可选)如果你想让它们在游戏运行起来的一瞬间直接“休眠”，不再消耗性能，
        // 可以取消下面这一行的注释(删掉前面的 // 即可)
        // 但这一步不会影响静态效果。
        // enabled = false; 
    }
}
