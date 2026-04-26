# 蜜蜂行为仿真演示流程（简化版）

## 1. 演示目标

演示目标应尽量简单：
1. 展示蜜蜂能自主飞向花朵/目标点。
2. 展示接近目标时减速、悬停、短暂停留。
3. 展示完成一轮访问后继续循环飞行。
4. 展示重复轮次中出现更稳定的访问顺序（路线记忆效果）。
5. 展示轨迹线、调试射线、可视化开关。

> 适合用于答辩、项目汇报、软著说明材料截图。

---

## 2. 推荐场景

- 推荐场景：`Assets/Scenes/Demo.unity`

---

## 3. 场景中需要的核心组件

### 3.1 蜜蜂对象
挂载以下脚本：
- `BeeSimulation`
- `BeeTargetController`
- （可选）`LineRenderer`，用于轨迹显示

### 3.2 管理对象
建议场景中存在：
- `BeeSimulationManager`
- `VisualizationManager`
- `CameraFollow`
- `SimplePauseManager`

### 3.3 目标对象
- 在场景中放置 3~5 个花朵或空物体作为采集目标。
- 将这些目标拖入 `BeeTargetController.targets` 列表。

---

## 4. 推荐 Inspector 参数

### BeeSimulation
- `controlMode = AutopilotTargets`
- `moveSpeed = 2 ~ 3`
- `maxSpeed = 3 ~ 4`
- `slowingRadius = 2.5 ~ 4`
- `showDebugRays = true`
- `showForces = true`

### BeeTargetController
- `enableHover = true`
- `stayDuration = 1.5 ~ 3`
- `hoverRadius = 1 ~ 2`
- `hoverSpeed = 0.5 ~ 1`
- `enableTargetCycleLoop = true`
- `enableStableRouteReplay = true`

---

## 5. 建议演示话术

### 第一阶段：基础行为
- 蜜蜂会自动搜索目标并飞向目标点。
- 接近目标后会减速并进行悬停。
- 该过程体现的是**基于行为规则的虚拟仿真**，不是严格生物力学求解。

### 第二阶段：环境响应
- 打开调试射线与轨迹线，展示避障和路径变化。
- 说明系统通过简单环境感知实现较自然的飞行效果。

### 第三阶段：路线循环
- 当所有目标访问完一轮后，蜜蜂不会停住，而会继续进入下一轮。
- 后续轮次会优先复用前一轮形成的访问顺序，表现出简化的“路线记忆”效果。

---

## 6. 适合截图的画面

推荐保存以下截图：
1. 蜜蜂飞向花朵的过程图
2. 悬停在花朵上方的过程图
3. 开启轨迹线后的路径图
4. 调试射线显示的避障图
5. 多个目标点组成的路线循环图

---

## 7. 对外说明模板

可直接用于答辩或文档：

“本系统为基于生物行为启发的蜜蜂飞行虚拟仿真。系统采用目标趋近、障碍规避、悬停停留、路线循环与可视化轨迹等轻量算法，在 Unity 中实现具有解释性和实时性的行为模拟效果，适用于教学演示、方案展示与软著材料整理。”
