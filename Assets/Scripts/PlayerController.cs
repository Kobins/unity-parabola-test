using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

    private Rigidbody m_rigidbody;
    private LineRenderer m_lineRenderer;
    
    [Header("Childs")]
    public Camera m_camera;
    public Transform m_topParent;
    public Transform m_barrelParent;

    [Header("Shoot")] 
    // 수식에서 발사 힘 p
    public float m_shootPower = 10f;
    public bool m_shootGreaterAngle = true;
    public GameObject m_bombPrefab;

    // 수식에서의 중력가속도 g
    // 중력은 연직 방향으로만 작용하는 것으로 가정
    // 또한 유니티의 gravity는 아래로 향하는 벡터이기 때문에 크기값으로 전환
    private readonly float m_gravityFactor = Math.Abs(Physics.gravity.y);

    [Header("Move")] 
    public float m_moveSpeed = 10f;
    public float m_steerSpeed = 5f;

    [Header("Extra")] 
    public Text m_stateText;

    private void UpdateStateText() {
        m_stateText.text = $"고각 여부: {m_shootGreaterAngle} (Tab)\n" +
                           $"발사 힘: {m_shootPower} (스크롤 조정)"
                           ;
    }
    
    private int m_terrainMask;
    private void Awake() {
        m_rigidbody = GetComponent<Rigidbody>();
        m_lineRenderer = GetComponent<LineRenderer>();
        m_terrainMask = LayerMask.GetMask("Terrain");
        UpdateStateText();
    }

    private void FixedUpdate() {
        var forward = Input.GetAxis("Vertical");
        var steer = Input.GetAxis("Horizontal") * (forward >= 0 ? 1f : -1f);
        
        m_rigidbody.MovePosition(m_rigidbody.position + transform.forward * (forward * m_moveSpeed * Time.deltaTime));
        m_rigidbody.angularVelocity = Vector3.zero;
        m_rigidbody.AddTorque(0f, steer * m_steerSpeed * Time.deltaTime, 0f, ForceMode.VelocityChange);
    }

    void Update() {
        // 고각/저각 변경
        if (Input.GetKeyDown(KeyCode.Tab)) {
            m_shootGreaterAngle = !m_shootGreaterAngle;
            UpdateStateText();
        }

        // 발사 힘 조정
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0) {
            m_shootPower += scroll * (Input.GetKey(KeyCode.LeftShift) ? 5 : 1);
            UpdateStateText();
        }
        
        // 마우스가 가리키는 지점을 raycast
        var ray = m_camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hitInfo, float.MaxValue, m_terrainMask)) {
            var target = hitInfo.point;
            // 포탑 yaw(y축) 회전
            var topOrigin = m_topParent.position;
            var horizontalDirection = target - topOrigin;
            horizontalDirection.y = 0;
            var horizontalLength = horizontalDirection.magnitude;
            horizontalDirection.Normalize();
            m_topParent.rotation = Quaternion.LookRotation(horizontalDirection);

            
            // 포신 pitch(x축) 회전
            var angleInRadian = Parabola.GetParabolaShootAngleInRadian(
                m_barrelParent.position, 
                target, 
                m_shootPower, 
                m_gravityFactor,
                m_shootGreaterAngle
            );
            // 발사가 가능한 위치인 경우
            // 발사각이 NaN이면 발사 불가능
            if (!float.IsNaN(angleInRadian)) {
                // 유니티의 x축 회전은 값이 증가할 수록 각이 내려가므로 부호 반전
                m_barrelParent.localRotation = Quaternion.Euler(-angleInRadian * Mathf.Rad2Deg, 0f, 0f);

                // 마우스 클릭 시 발사
                if (Input.GetMouseButtonDown(0)) {
                    Shoot();        
                }
                
                // 포물선 그리기 (LineRenderer)
                m_lineRenderer.enabled = true;
                var barrelOrigin = m_barrelParent.position;
                var v0x = m_shootPower * Mathf.Cos(angleInRadian);  // 초기 발사 벡터 x
                var v0y = m_shootPower * Mathf.Sin(angleInRadian);  // 초기 발사 벡터 y
                var a = -(m_gravityFactor) / (2 * v0x.Square());    // x^2 계수
                var tanAngle = v0y / v0x;                           // x 계수

                const float divide = 0.2f;
                var count = Mathf.CeilToInt(horizontalLength / divide);
                m_lineRenderer.positionCount = count;
                var list = new Vector3[count];
                float xPrime = 0f;
                for (int i = 0; i < count; ++i) {
                    // 월드 위치 + x'축 값 + y축 값
                    var relativePosition = horizontalDirection * xPrime;   // x'축 값
                    relativePosition.y = a * xPrime.Square() + tanAngle * xPrime;  // y축 값
                    list[i] = barrelOrigin + relativePosition;                     // 월드 위치
                    xPrime += divide;
                }
                m_lineRenderer.SetPositions(list);
            }
            else {
                m_lineRenderer.enabled = false;
            }

        }
        else {
            m_lineRenderer.enabled = false;
        }
    }

    private void Shoot() {
        var instance = Instantiate(m_bombPrefab, m_barrelParent.position, Quaternion.identity);
        var rigidbody = instance.GetComponent<Rigidbody>();
        // 발사 전에 m_topParent의 yaw와 m_barrelParent의 pitch가 조정된 상태여야 함
        var direction = m_barrelParent.forward;
        direction.Normalize();
        // 거기에 m_shootPower를 곱하면 발사 벡터 완성
        // 발사 벡터는 초기 속도를 결정하므로, 질량과 관계없이 속도를 즉시 변경
        rigidbody.AddForce(direction * m_shootPower, ForceMode.VelocityChange);
        var bomb = instance.GetComponent<BombController>();
        bomb.Initialize(m_lineRenderer);
    }
    
}
