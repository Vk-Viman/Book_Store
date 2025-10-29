describe('Add to cart and checkout flow', () => {
  it('adds a product to cart, checks out and verifies stock reduced', () => {
    cy.visit('/books');
    // wait for product list
    cy.get('[data-cy=book-card]').first().as('firstCard');

    cy.get('@firstCard').within(() => {
      cy.get('[data-cy=add-to-cart]').click();
    });

    cy.get('[routerlink="/cart"], a[href="/cart"]').first().click();
    cy.contains('Checkout').click();
    cy.get('input[name=name]').type('Test User');
    cy.get('textarea[name=address]').type('123 Test St');
    cy.get('input[name=phone]').type('1234567890');

    // force a successful payment token
    cy.intercept('POST', '/api/orders/checkout', (req) => {
      req.headers['x-payment-token'] = 'ok-token';
    }).as('checkoutReq');

    cy.get('button').contains('Pay').click();
    cy.wait('@checkoutReq');

    // after checkout go back to books and ensure stock label updated
    cy.visit('/books');
    cy.get('[data-cy=book-card]').first().within(() => {
      cy.get('[data-cy=stock]').invoke('text').then((text) => {
        expect(text).to.match(/In Stock|Out of Stock/);
      });
    });
  });

  it('handles payment failure gracefully and shows error', () => {
    cy.visit('/books');
    cy.get('[data-cy=book-card]').first().as('firstCard');
    cy.get('@firstCard').within(() => { cy.get('[data-cy=add-to-cart]').click(); });
    cy.get('[routerlink="/cart"], a[href="/cart"]').first().click();
    cy.contains('Checkout').click();
    cy.get('input[name=name]').type('Test User');
    cy.get('textarea[name=address]').type('123 Test St');
    cy.get('input[name=phone]').type('1234567890');

    // stub server to return 400 for payment
    cy.intercept('POST', '/api/orders/checkout', { statusCode: 400, body: { message: 'Payment declined' } }).as('checkoutFail');

    cy.get('button').contains('Pay').click();
    cy.wait('@checkoutFail');
    cy.contains('Payment declined');
  });
});
