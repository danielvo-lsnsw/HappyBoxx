import { toCsv } from './csv';

describe('toCsv', () => {
  it('quotes special characters and neutralises formulas', () => {
    const csv = toCsv(
      [{ name: 'Apple, Red', note: '=HYPERLINK("x")' }],
      [
        { header: 'Name', value: (r) => r.name },
        { header: 'Note', value: (r) => r.note },
      ],
    );

    expect(csv).toBe('Name,Note\r\n"Apple, Red","\'=HYPERLINK(""x"")"');
  });
});
