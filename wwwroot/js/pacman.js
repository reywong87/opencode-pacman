const tile = 20;
const directions = { left: [-1, 0], right: [1, 0], up: [0, -1], down: [0, 1] };
const keys = { ArrowLeft: "left", ArrowRight: "right", ArrowUp: "up", ArrowDown: "down" };

export function start(canvas, dotnet) {
  const context = canvas.getContext("2d");
  let frame = 0;
  window.addEventListener("keydown", event => {
    const direction = keys[event.key];
    if (!direction) return;
    event.preventDefault();
    dotnet.invokeMethodAsync("SetDirection", direction);
  });
  const loop = async () => {
    draw(context, await dotnet.invokeMethodAsync("Tick"), frame++);
    requestAnimationFrame(loop);
  };
  loop();
}

function center(x, y) { return [x * tile + tile / 2, y * tile + tile / 2]; }
function draw(context, game, frame) {
  const grid = game.grid, width = grid[0].length;
  context.fillStyle = "#000"; context.fillRect(0, 0, width * tile, grid.length * tile);
  drawWalls(context, grid); drawDoor(context, grid); drawDots(context, grid);
  drawPacman(context, game.pacman, frame);
  game.ghosts.forEach((ghost, index) => drawGhost(context, ghost, ["#f00", "#0ff", "#ffb8ff", "#ffb852"][index]));
  context.fillStyle = "#fff"; context.font = '14px "Courier New", monospace'; context.textBaseline = "top";
  context.textAlign = "left"; context.fillText(`SCORE ${game.score}`, 8, 4); context.textAlign = "right"; context.fillText(`VIDAS ${game.lives}`, width * tile - 8, 4);
}
function drawWalls(context, grid) {
  context.strokeStyle = "#2121ff"; context.lineWidth = 2.5; context.lineCap = context.lineJoin = "round"; context.beginPath();
  grid.forEach((row, y) => row.forEach((cell, x) => { if (cell !== 1) return; const [cx, cy] = center(x, y); if (grid[y][x + 1] === 1) { context.moveTo(cx, cy); context.lineTo(cx + tile, cy); } if (grid[y + 1]?.[x] === 1) { context.moveTo(cx, cy); context.lineTo(cx, cy + tile); } })); context.stroke();
}
function drawDoor(context, grid) { context.strokeStyle = "#ffb8ff"; context.lineWidth = 3; context.beginPath(); grid.forEach((row, y) => row.forEach((cell, x) => { if (cell === 3) { context.moveTo(x * tile, y * tile + 10); context.lineTo((x + 1) * tile, y * tile + 10); } })); context.stroke(); }
function drawDots(context, grid) { context.fillStyle = "#ffb897"; grid.forEach((row, y) => row.forEach((cell, x) => { if (cell === 2) { const [cx, cy] = center(x, y); context.beginPath(); context.arc(cx, cy, 2.5, 0, Math.PI * 2); context.fill(); } })); }
function drawPacman(context, pacman, frame) { const [cx, cy] = center(pacman.x, pacman.y); const rotation = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 }[pacman.direction]; const open = (Math.sin(frame * .3) * .5 + .5) * .28 + .02; context.fillStyle = "#ff0"; context.beginPath(); context.moveTo(cx, cy); context.arc(cx, cy, 9, rotation + open * Math.PI, rotation - open * Math.PI); context.closePath(); context.fill(); }
function drawGhost(context, ghost, color) { const [cx, cy] = center(ghost.x, ghost.y), radius = 9; context.fillStyle = color; context.beginPath(); context.arc(cx, cy - 1, radius, Math.PI, 0); context.lineTo(cx + radius, cy + radius); context.lineTo(cx + 3, cy + 5); context.lineTo(cx, cy + radius); context.lineTo(cx - 3, cy + 5); context.lineTo(cx - radius, cy + radius); context.closePath(); context.fill(); const [dx, dy] = directions[ghost.direction] ?? [0, 0]; [-3.5, 3.5].forEach(offset => { context.fillStyle = "#fff"; context.beginPath(); context.arc(cx + offset, cy - 1, 3, 0, Math.PI * 2); context.fill(); context.fillStyle = "#0000bb"; context.beginPath(); context.arc(cx + offset + dx * 1.6, cy - 1 + dy * 1.6, 1.5, 0, Math.PI * 2); context.fill(); }); }
