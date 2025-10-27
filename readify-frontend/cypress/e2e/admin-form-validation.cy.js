describe('Admin product form validation', () => {
  beforeEach(() => {
    cy.visit('/login');
    cy.get('input[name="email"]').type('admin@readify.local');
    cy.get('input[name="password"]').type('Readify#Admin123!');
    cy.get('button[type="submit"]', { timeout: 10000 }).should('be.visible').click();
    cy.contains('Admin', { timeout: 10000 }).click();
    cy.contains('Add product', { timeout: 10000 }).click();
  });

  it('shows error when category not selected', () => {
    cy.get('input[formcontrolname="title"]').type('Test Product');
    cy.get('input[formcontrolname="price"]').clear().type('1.00');
    cy.get('input[formcontrolname="stockQty"]').clear().type('1');
    cy.get('button[type="submit"]').click();
    cy.contains('Please select a valid category', { timeout: 5000 }).should('exist');
  });

  it('rejects invalid image URL', () => {
    cy.get('input[formcontrolname="title"]').type('Test Product');
    cy.get('input[formcontrolname="price"]').clear().type('1.00');
    cy.get('input[formcontrolname="stockQty"]').clear().type('1');
    cy.get('select').select(1);
    cy.get('input[formcontrolname="imageUrl"]').type('https://example.com');
    cy.get('button[type="submit"]').click();
    // should show image validation error
    cy.contains('Image URL did not load as an image', { timeout: 10000 }).should('exist');
  });
});
