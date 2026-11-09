import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

describe('Pagination', () => {
  let fixture: ComponentFixture<Pagination>;
  let element: HTMLElement;

  async function render(page: number, pageSize: number, totalCount: number) {
    fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('totalCount', totalCount);
    await fixture.whenStable();
    element = fixture.nativeElement;
  }

  const previous = () => element.querySelector<HTMLButtonElement>('[aria-label="Previous page"]')!;
  const next = () => element.querySelector<HTMLButtonElement>('[aria-label="Next page"]')!;

  it('shows the range of the current page', async () => {
    await render(2, 10, 14);
    expect(element.textContent).toContain('11–14 of 14');
  });

  it('disables previous on the first page and next on the last', async () => {
    await render(1, 10, 14);
    expect(previous().disabled).toBe(true);
    expect(next().disabled).toBe(false);

    await render(2, 10, 14);
    expect(previous().disabled).toBe(false);
    expect(next().disabled).toBe(true);
  });

  it('emits the next page', async () => {
    await render(1, 10, 14);
    let emitted: number | undefined;
    fixture.componentInstance.pageChange.subscribe((page) => (emitted = page));

    next().click();

    expect(emitted).toBe(2);
  });

  it('emits the new page size', async () => {
    await render(1, 10, 14);
    let emitted: number | undefined;
    fixture.componentInstance.pageSizeChange.subscribe((size) => (emitted = size));

    const select = element.querySelector('select')!;
    select.value = '20';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toBe(20);
  });

  it('shows 0–0 when there is nothing to page through', async () => {
    await render(1, 10, 0);
    expect(element.textContent).toContain('0–0 of 0');
    expect(next().disabled).toBe(true);
  });
});
