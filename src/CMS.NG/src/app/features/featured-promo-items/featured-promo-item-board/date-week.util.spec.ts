import { addDays, dayHeading, mmdd, mondayOf, toIso } from './date-week.util';

describe('date-week util', () => {
  it('mondayOf returns the same day when given a Monday', () => {
    expect(mondayOf('2026-03-16')).toBe('2026-03-16'); // 2026-03-16 is a Monday
  });

  it('mondayOf snaps mid-week and Sunday back to that week Monday', () => {
    expect(mondayOf('2026-03-19')).toBe('2026-03-16'); // Thursday
    expect(mondayOf('2026-03-22')).toBe('2026-03-16'); // Sunday belongs to the same week
  });

  it('mondayOf rolls a Monday-of-next-week correctly', () => {
    expect(mondayOf('2026-03-23')).toBe('2026-03-23');
  });

  it('addDays crosses month boundaries', () => {
    expect(addDays('2026-03-29', 7)).toBe('2026-04-05');
    expect(addDays('2026-03-16', -7)).toBe('2026-03-09');
  });

  it('the week is exactly Monday..Sunday (7 days)', () => {
    const start = '2026-03-16';
    const days = Array.from({ length: 7 }, (_, i) => addDays(start, i));
    expect(days[0]).toBe('2026-03-16'); // Mon
    expect(days[6]).toBe('2026-03-22'); // Sun
    expect(addDays(start, 6)).toBe(days[6]);
  });

  it('mmdd drops leading zeros', () => {
    expect(mmdd('2026-03-16')).toBe('3/16');
    expect(mmdd('2026-03-01')).toBe('3/1');
  });

  it('dayHeading pairs the date with the Mon-based weekday label', () => {
    expect(dayHeading('2026-03-16', 0)).toBe('3/16 (一)');
    expect(dayHeading('2026-03-22', 6)).toBe('3/22 (日)');
  });

  it('toIso zero-pads month and day', () => {
    expect(toIso(new Date(2026, 0, 5, 12))).toBe('2026-01-05');
  });
});
