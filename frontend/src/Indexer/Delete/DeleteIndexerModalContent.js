import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

class DeleteIndexerModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      deleteFiles: false,
      addImportExclusion: false
    };
  }

  //
  // Listeners

  onDeleteFilesChange = ({ value }) => {
    this.setState({ deleteFiles: value });
  };

  onAddImportExclusionChange = ({ value }) => {
    this.setState({ addImportExclusion: value });
  };

  onDeleteMovieConfirmed = () => {
    const deleteFiles = this.state.deleteFiles;
    const addImportExclusion = this.state.addImportExclusion;

    this.setState({ deleteFiles: false, addImportExclusion: false });
    this.props.onDeletePress(deleteFiles, addImportExclusion);
  };

  //
  // Render

  render() {
    const {
      name,
      onModalClose
    } = this.props;

    return (
      <ModalContent
        onModalClose={onModalClose}
      >
        <ModalHeader>
          Delete - {name}
        </ModalHeader>

        <ModalBody>
          {`Are you sure you want to delete ${name} from Prowlarr`}
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Close')}
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
  name: PropTypes.string.isRequired,
  onDeletePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default DeleteIndexerModalContent;
