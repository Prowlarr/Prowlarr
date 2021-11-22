import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './DeleteIndexerModalContent.css';

class DeleteIndexerModalContent extends Component {
  onDeleteMovieConfirmed = () => {
    this.props.onDeleteSelectedPress();
  };

  //
  // Render

  render() {
    const {
      indexers,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Delete Selected Indexers(s)
        </ModalHeader>

        <ModalBody>

          <div className={styles.message}>
            {`Are you sure you want to delete ${indexers.length} selected indexers(s)`}
          </div>

          <ul>
            {
              indexers.map((s) => {
                return (
                  <li key={s.name}>
                    <span>{s.name}</span>
                  </li>
                );
              })
            }
          </ul>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Cancel')}
          </Button>

          <Button
            kind={kinds.DANGER}
            onPress={this.onDeleteMovieConfirmed}
          >
            {translate('Delete')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

DeleteIndexerModalContent.propTypes = {
  indexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteSelectedPress: PropTypes.func.isRequired
};

export default DeleteIndexerModalContent;
