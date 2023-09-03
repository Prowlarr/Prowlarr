export default function abbreviateNumber(num, decimalPlaces) {
  if (num === null) {
    return null;
  }

  if (num === 0) {
    return '0';
  }

  decimalPlaces = (!decimalPlaces || decimalPlaces < 0) ? 0 : decimalPlaces;

  const b = (num).toPrecision(2).split('e');
  const k = b.length === 1 ? 0 : Math.floor(Math.min(b[1].slice(1), 14) / 3);
  const c = k < 1 ? num.toFixed(0 + decimalPlaces) : (num / Math.pow(10, k * 3) ).toFixed(1 + decimalPlaces);
  const d = c < 0 ? c : Math.abs(c);
  const e = d + ['', 'K', 'M', 'B', 'T'][k];

  return e;
}
