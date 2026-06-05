interface Props {
  data: ReadonlyArray<number>;
  width?: number;
  height?: number;
  color?: string;
  /** Fill the area under the line at low opacity. */
  area?: boolean;
}

export default function Sparkline({
  data,
  width = 86,
  height = 28,
  color = 'var(--fg-2)',
  area = true,
}: Props) {
  if (!data || data.length < 2) {
    return (
      <svg
        width={width}
        height={height}
        aria-hidden="true"
        style={{ display: 'block', overflow: 'visible' }}
      />
    );
  }

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;
  const stepX = width / (data.length - 1);

  const points = data.map((v, i) => {
    const x = i * stepX;
    const y = height - ((v - min) / range) * (height - 4) - 2;
    return [x, y] as const;
  });

  const linePath = points
    .map(([x, y], i) => `${i === 0 ? 'M' : 'L'}${x.toFixed(2)},${y.toFixed(2)}`)
    .join(' ');

  const areaPath = `${linePath} L${width},${height} L0,${height} Z`;

  return (
    <svg
      width={width}
      height={height}
      aria-hidden="true"
      style={{ display: 'block', overflow: 'visible' }}
    >
      {area && (
        <path d={areaPath} fill={color} opacity={0.08} />
      )}
      <path
        d={linePath}
        fill="none"
        stroke={color}
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
