import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import translate from 'Utilities/String/translate';
import AddApplicationItem from './AddApplicationItem';
import styles from './AddApplicationModalContent.css';

class AddApplicationModalContent extends Component {

  //
  // Render

  render() {
    const {
      isSchemaFetching,
      isSchemaPopulated,
      schemaError,
      schema,
      onApplicationSelect,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddApplication')}
        </ModalHeader>

        <ModalBody>
          {
            isSchemaFetching &&
              <LoadingIndicator />
          }

          {
            !isSchemaFetching && !!schemaError &&
              <div>
                {translate('UnableToAddANewApplicationPleaseTryAgain')}
              </div>
          }

          {
            isSchemaPopulated && !schemaError &&
              <div>
                <div className={styles.applications}>
                  {
                    schema.map((application) => {
                      return (
                        <AddApplicationItem
                          key={application.implementation}
                          implementation={application.implementation}
                          {...application}
                          onApplicationSelect={onApplicationSelect}
                        />
                      );
                    })
                  }
                </div>
              </div>
          }
        </ModalBody>
        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            {translate('Close')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddApplicationModalContent.propTypes = {
  isSchemaFetching: PropTypes.bool.isRequired,
  isSchemaPopulated: PropTypes.bool.isRequired,
  schemaError: PropTypes.object,
  schema: PropTypes.arrayOf(PropTypes.object).isRequired,
  onApplicationSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddApplicationModalContent;
