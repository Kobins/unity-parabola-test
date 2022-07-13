using System;
using UnityEngine;

public static class Parabola {

    public static float Square(this float value) 
        => value * value;
    
    /// <summary>
    /// 목표 지점에 도달하도록 하는 포물선의 발사각을 구합니다.
    /// https://www.desmos.com/calculator/os6hmlpkm0를 참고하세요.
    /// </summary>
    /// <param name="origin">발사 시작점입니다.</param>
    /// <param name="target">발사 목표점입니다.</param>
    /// <param name="shootPower">발사 힘입니다.</param>
    /// <param name="gravity">중력 가속도 크기입니다.</param>
    /// <param name="greaterAngle">true일 경우 각이 큰 쪽을, false일 경우 작은 쪽을 반환합니다.</param>
    /// <returns>만족하는 발사각을 라디안으로 반환합니다. 현재 조건으로 발사가 불가능할 경우 NaN을 반환합니다.</returns>
    public static float GetParabolaShootAngleInRadian(
        Vector3 origin, Vector3 target, 
        float shootPower, float gravity, bool greaterAngle = true
    ) {
        var relativeTarget = target - origin;
        // y축과 목표 지점 벡터가 이루는 평면 상으로 축소
        var x1Square = relativeTarget.x.Square() + relativeTarget.z.Square();
        var x1 = Mathf.Sqrt(x1Square);
        var y1 = relativeTarget.y;

        var k = -(gravity * x1Square) / (2 * shootPower.Square());
        // 판별식(제곱근 안 식)
        var determiner = x1Square - 4f * k * (k - y1);
        
        // 없을 수도 있음
        if (determiner < 0) {
            return float.NaN;
        }
        var sign = greaterAngle ? -1f : 1f;
        var angle = Mathf.Atan(
            (-x1 + Mathf.Sqrt(determiner) * sign) / (2f * k)
        );
        return angle;
    }
}